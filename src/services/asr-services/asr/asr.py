"""
ASR (Automatic Speech Recognition) Architecture for Industrial OT Environments
================================================================================

Complete implementation with:
- RNNoise integration for noise suppression
- WebRTC VAD for voice activity detection
- PostgreSQL database for vocabulary management
- Redis caching for performance
- Whisper ASR engine
- FastAPI REST service

Stack:
- Whisper (OpenAI) for speech-to-text
- RNNoise for industrial noise suppression
- WebRTC VAD for voice activity detection
- PostgreSQL for vocabulary persistence
- Redis for caching
- FastAPI for API service
"""

from fastapi import FastAPI, File, UploadFile, HTTPException, Depends
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field
from typing import List, Optional, Dict
import numpy as np
import torch
import whisper
import io
import wave
import json
import yaml
from datetime import datetime
from enum import Enum
import logging
import asyncio
from contextlib import asynccontextmanager
import os
from pathlib import Path

# Database and caching
import asyncpg
import redis.asyncio as redis
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.ext.asyncio import async_sessionmaker
from sqlalchemy.orm import declarative_base
from sqlalchemy import Column, Integer, String, Float, DateTime, JSON, Boolean, ForeignKey
import hashlib

# Audio processing
import webrtcvad
import struct
import subprocess
import tempfile

# ============================================================================
# CONFIGURATION
# ============================================================================

def load_config(config_path: Optional[str] = None) -> dict:
    """Load configuration from YAML file with environment variable overrides"""
    
    # Determine config file path
    if config_path is None:
        config_path = os.getenv("ASR_CONFIG_PATH", 
                                Path(__file__).parent / "config.yaml")
    
    config_path = Path(config_path)
    
    if not config_path.exists():
        raise FileNotFoundError(f"Configuration file not found: {config_path}")
    
    with open(config_path, 'r') as f:
        config = yaml.safe_load(f)
    
    return config


class ASRConfig:
    """Configuration for ASR system loaded from external config file"""
    
    def __init__(self, config_path: Optional[str] = None):
        # Load from YAML file
        self._config = load_config(config_path)
        
        # Model settings
        self.WHISPER_MODEL = os.getenv("WHISPER_MODEL", 
                                       self._config["model"]["name"])
        self.WHISPER_LANGUAGE = self._config["model"]["language"]
        self.WHISPER_TASK = self._config["model"]["task"]
        self.DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
        
        # Audio preprocessing
        self.SAMPLE_RATE = self._config["audio"]["sample_rate"]
        self.MAX_AUDIO_SIZE_MB = self._config["audio"]["max_size_mb"]
        
        # RNNoise settings
        self.RNNOISE_ENABLED = self._get_bool_env(
            "RNNOISE_ENABLED", 
            self._config["rnnoise"]["enabled"]
        )
        self.RNNOISE_PATH = os.getenv("RNNOISE_PATH", 
                                      self._config["rnnoise"]["binary_path"])
        
        # VAD settings
        self.VAD_ENABLED = self._get_bool_env(
            "VAD_ENABLED", 
            self._config["vad"]["enabled"]
        )
        self.VAD_AGGRESSIVENESS = int(os.getenv(
            "VAD_AGGRESSIVENESS", 
            self._config["vad"]["aggressiveness"]
        ))
        self.VAD_FRAME_DURATION_MS = self._config["vad"]["frame_duration_ms"]
        
        # Semantic filtering
        self.CONFIDENCE_THRESHOLD = float(os.getenv(
            "CONFIDENCE_THRESHOLD",
            self._config["semantic"]["confidence_threshold"]
        ))
        self.MAX_ALTERNATIVES = self._config["semantic"]["max_alternatives"]
        self.ALLOWED_VOCABULARIES = self._config["semantic"]["allowed_vocabularies"]
        
        # Database settings
        db = self._config["database"]
        db_host = os.getenv("DB_HOST", db["host"])
        db_port = os.getenv("DB_PORT", db["port"])
        db_name = os.getenv("DB_NAME", db["name"])
        db_user = os.getenv("DB_USER", db["user"])
        db_password = os.getenv("DB_PASSWORD", db["password"])
        
        self.DATABASE_URL = os.getenv(
            "DATABASE_URL",
            f"postgresql+asyncpg://{db_user}:{db_password}@{db_host}:{db_port}/{db_name}"
        )
        self.DB_POOL_SIZE = db["pool_size"]
        self.DB_MAX_OVERFLOW = db["max_overflow"]
        
        # Redis settings
        cache = self._config["cache"]
        redis_host = os.getenv("REDIS_HOST", cache["host"])
        redis_port = os.getenv("REDIS_PORT", cache["port"])
        redis_db = os.getenv("REDIS_DB", cache["db"])
        
        self.REDIS_URL = os.getenv(
            "REDIS_URL",
            f"redis://{redis_host}:{redis_port}/{redis_db}"
        )
        self.CACHE_TTL_SECONDS = int(os.getenv(
            "CACHE_TTL_SECONDS",
            cache["ttl_seconds"]
        ))
        
        # API settings
        api = self._config["api"]
        self.API_HOST = os.getenv("API_HOST", api["host"])
        self.API_PORT = int(os.getenv("API_PORT", api["port"]))
        self.API_TIMEOUT_SECONDS = api["timeout_seconds"]
        self.LOG_LEVEL = os.getenv("LOG_LEVEL", api["log_level"])
    
    @staticmethod
    def _get_bool_env(key: str, default: bool) -> bool:
        """Get boolean from environment variable"""
        env_value = os.getenv(key)
        if env_value is None:
            return default
        return env_value.lower() in ("true", "1", "yes", "on")
    
    def to_dict(self) -> dict:
        """Return configuration as dictionary"""
        return {
            "model": {
                "name": self.WHISPER_MODEL,
                "language": self.WHISPER_LANGUAGE,
                "device": self.DEVICE
            },
            "audio": {
                "sample_rate": self.SAMPLE_RATE,
                "max_size_mb": self.MAX_AUDIO_SIZE_MB
            },
            "rnnoise": {
                "enabled": self.RNNOISE_ENABLED,
                "path": self.RNNOISE_PATH
            },
            "vad": {
                "enabled": self.VAD_ENABLED,
                "aggressiveness": self.VAD_AGGRESSIVENESS,
                "frame_duration_ms": self.VAD_FRAME_DURATION_MS
            },
            "semantic": {
                "confidence_threshold": self.CONFIDENCE_THRESHOLD,
                "max_alternatives": self.MAX_ALTERNATIVES,
                "allowed_vocabularies": self.ALLOWED_VOCABULARIES
            },
            "cache": {
                "ttl_seconds": self.CACHE_TTL_SECONDS
            }
        }


# ============================================================================
# DATABASE MODELS
# ============================================================================

Base = declarative_base()

class Vocabulary(Base):
    """Vocabulary table"""
    __tablename__ = "vocabularies"
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), unique=True, nullable=False, index=True)
    description = Column(String(500))
    category = Column(String(100))
    active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    metadata_json = Column(JSON)


class VocabularyTerm(Base):
    """Vocabulary terms table"""
    __tablename__ = "vocabulary_terms"
    
    id = Column(Integer, primary_key=True, index=True)
    vocabulary_id = Column(Integer, ForeignKey("vocabularies.id"), nullable=False)
    term = Column(String(200), nullable=False, index=True)
    normalized_term = Column(String(200), index=True)  # Lowercase, trimmed
    confidence_weight = Column(Float, default=1.0)
    synonyms = Column(JSON)  # List of synonyms
    metadata_json = Column(JSON)
    active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)


class TranscriptionLog(Base):
    """Log of all transcriptions"""
    __tablename__ = "transcription_logs"
    
    id = Column(Integer, primary_key=True, index=True)
    audio_hash = Column(String(64), index=True)  # SHA256 of audio
    transcribed_text = Column(String(2000))
    confidence = Column(Float)
    language = Column(String(10))
    duration_seconds = Column(Float)
    processing_time_ms = Column(Float)
    matched_vocabulary = Column(String(100))
    matched_terms = Column(JSON)
    status = Column(String(50))
    warnings = Column(JSON)
    created_at = Column(DateTime, default=datetime.utcnow, index=True)
    metadata_json = Column(JSON)


# ============================================================================
# DATA MODELS
# ============================================================================

class TranscriptionStatus(str, Enum):
    SUCCESS = "success"
    LOW_CONFIDENCE = "low_confidence"
    NO_MATCH = "no_match"
    ERROR = "error"


class TranscriptionResult(BaseModel):
    text: str
    confidence: float = Field(ge=0.0, le=1.0)
    language: Optional[str] = None
    duration: Optional[float] = None
    timestamp: str = Field(default_factory=lambda: datetime.utcnow().isoformat())
    
    
class SemanticMatch(BaseModel):
    matched_term: str
    original_text: str
    confidence: float
    vocabulary: str
    metadata: Optional[Dict] = None


class ASRResponse(BaseModel):
    status: TranscriptionStatus
    transcription: Optional[TranscriptionResult] = None
    semantic_matches: List[SemanticMatch] = []
    alternatives: List[str] = []
    processing_time_ms: float
    warnings: List[str] = []
    cached: bool = False


class VocabularyCreate(BaseModel):
    name: str
    description: Optional[str] = None
    category: Optional[str] = None
    terms: List[str]
    metadata: Optional[Dict] = None


class VocabularyUpdate(BaseModel):
    description: Optional[str] = None
    category: Optional[str] = None
    active: Optional[bool] = None
    metadata: Optional[Dict] = None


# ============================================================================
# DATABASE MANAGER
# ============================================================================

class DatabaseManager:
    """Manages database connections and operations"""
    
    def __init__(self, config: ASRConfig):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self.engine = None
        self.session_maker = None
        
    async def initialize(self):
        """Initialize database connection"""
        try:
            self.engine = create_async_engine(
                self.config.DATABASE_URL,
                echo=False,
                pool_size=self.config.DB_POOL_SIZE,
                max_overflow=self.config.DB_MAX_OVERFLOW
            )
            
            self.session_maker = async_sessionmaker(
                self.engine,
                class_=AsyncSession,
                expire_on_commit=False
            )
            
            # Create tables
            async with self.engine.begin() as conn:
                await conn.run_sync(Base.metadata.create_all)
            
            self.logger.info("Database initialized successfully")
            
            # Create default vocabularies if not exist
            await self._create_default_vocabularies()
            
        except Exception as e:
            self.logger.error(f"Failed to initialize database: {e}")
            raise
    
    async def _create_default_vocabularies(self):
        """Create default vocabularies"""
        default_vocabs = {
            "default": {
                "description": "Default command vocabulary",
                "category": "commands",
                "terms": ["inizio", "fine", "pausa", "continua", "stop", "aiuto"]
            },
            "parts": {
                "description": "Mechanical parts vocabulary",
                "category": "mechanical_parts",
                "terms": [
                    "vite", "dado", "bullone", "rondella", "guarnizione",
                    "cuscinetto", "albero", "ingranaggio", "puleggia"
                ]
            },
            "quality": {
                "description": "Quality control vocabulary",
                "category": "quality_control",
                "terms": [
                    "conforme", "non conforme", "difetto", "scarto",
                    "riparazione", "accettabile", "critico"
                ]
            },
            "maintenance": {
                "description": "Maintenance vocabulary",
                "category": "maintenance",
                "terms": [
                    "manutenzione", "lubrificazione", "ispezione", "pulizia",
                    "sostituzione", "calibrazione", "verifica"
                ]
            }
        }
        
        async with self.session_maker() as session:
            for name, data in default_vocabs.items():
                # Check if exists
                result = await session.execute(
                    f"SELECT id FROM vocabularies WHERE name = '{name}'"
                )
                if result.scalar_one_or_none():
                    continue
                
                # Create vocabulary
                vocab = Vocabulary(
                    name=name,
                    description=data["description"],
                    category=data["category"],
                    metadata_json={"default": True}
                )
                session.add(vocab)
                await session.flush()
                
                # Add terms
                for term in data["terms"]:
                    vocab_term = VocabularyTerm(
                        vocabulary_id=vocab.id,
                        term=term,
                        normalized_term=term.lower().strip()
                    )
                    session.add(vocab_term)
                
                await session.commit()
                self.logger.info(f"Created default vocabulary: {name}")
    
    async def get_vocabulary_terms(self, vocab_name: str) -> List[Dict]:
        """Get all terms for a vocabulary"""
        async with self.session_maker() as session:
            result = await session.execute(f"""
                SELECT vt.term, vt.normalized_term, vt.confidence_weight, 
                       vt.synonyms, vt.metadata_json, v.category
                FROM vocabulary_terms vt
                JOIN vocabularies v ON vt.vocabulary_id = v.id
                WHERE v.name = '{vocab_name}' AND v.active = true AND vt.active = true
            """)
            
            terms = []
            for row in result:
                terms.append({
                    "term": row[0],
                    "normalized_term": row[1],
                    "confidence_weight": row[2] or 1.0,
                    "synonyms": row[3] or [],
                    "metadata": row[4] or {},
                    "category": row[5]
                })
            
            return terms
    
    async def add_vocabulary(self, vocab_create: VocabularyCreate) -> int:
        """Add new vocabulary with terms"""
        async with self.session_maker() as session:
            # Create vocabulary
            vocab = Vocabulary(
                name=vocab_create.name,
                description=vocab_create.description,
                category=vocab_create.category,
                metadata_json=vocab_create.metadata or {}
            )
            session.add(vocab)
            await session.flush()
            
            # Add terms
            for term in vocab_create.terms:
                vocab_term = VocabularyTerm(
                    vocabulary_id=vocab.id,
                    term=term,
                    normalized_term=term.lower().strip()
                )
                session.add(vocab_term)
            
            await session.commit()
            self.logger.info(f"Added vocabulary: {vocab_create.name} with {len(vocab_create.terms)} terms")
            return vocab.id
    
    async def log_transcription(self, audio_hash: str, result: ASRResponse, 
                               matched_vocab: Optional[str] = None):
        """Log transcription result"""
        async with self.session_maker() as session:
            log = TranscriptionLog(
                audio_hash=audio_hash,
                transcribed_text=result.transcription.text if result.transcription else None,
                confidence=result.transcription.confidence if result.transcription else 0.0,
                language=result.transcription.language if result.transcription else None,
                duration_seconds=result.transcription.duration if result.transcription else 0.0,
                processing_time_ms=result.processing_time_ms,
                matched_vocabulary=matched_vocab,
                matched_terms=[m.dict() for m in result.semantic_matches],
                status=result.status.value,
                warnings=result.warnings
            )
            session.add(log)
            await session.commit()
    
    async def close(self):
        """Close database connection"""
        if self.engine:
            await self.engine.dispose()


# ============================================================================
# CACHE MANAGER
# ============================================================================

class CacheManager:
    """Manages Redis cache for transcriptions"""
    
    def __init__(self, config: ASRConfig):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self.redis = None
        
    async def initialize(self):
        """Initialize Redis connection"""
        try:
            self.redis = await redis.from_url(
                self.config.REDIS_URL,
                encoding="utf-8",
                decode_responses=True
            )
            await self.redis.ping()
            self.logger.info("Redis cache initialized successfully")
        except Exception as e:
            self.logger.error(f"Failed to initialize Redis: {e}")
            # Continue without cache
            self.redis = None
    
    def _get_cache_key(self, audio_hash: str, vocabularies: Optional[List[str]]) -> str:
        """Generate cache key"""
        vocab_key = ",".join(sorted(vocabularies)) if vocabularies else "all"
        return f"asr:transcription:{audio_hash}:{vocab_key}"
    
    async def get_cached_result(self, audio_hash: str, 
                                vocabularies: Optional[List[str]]) -> Optional[ASRResponse]:
        """Get cached transcription result"""
        if not self.redis:
            return None
        
        try:
            cache_key = self._get_cache_key(audio_hash, vocabularies)
            cached = await self.redis.get(cache_key)
            
            if cached:
                data = json.loads(cached)
                result = ASRResponse(**data)
                result.cached = True
                self.logger.info(f"Cache hit for {audio_hash}")
                return result
            
            return None
            
        except Exception as e:
            self.logger.warning(f"Cache retrieval error: {e}")
            return None
    
    async def cache_result(self, audio_hash: str, vocabularies: Optional[List[str]], 
                          result: ASRResponse):
        """Cache transcription result"""
        if not self.redis:
            return
        
        try:
            cache_key = self._get_cache_key(audio_hash, vocabularies)
            data = result.dict()
            data["cached"] = False  # Reset for storage
            
            await self.redis.setex(
                cache_key,
                self.config.CACHE_TTL_SECONDS,
                json.dumps(data, default=str)
            )
            
            self.logger.info(f"Cached result for {audio_hash}")
            
        except Exception as e:
            self.logger.warning(f"Cache storage error: {e}")
    
    async def close(self):
        """Close Redis connection"""
        if self.redis:
            await self.redis.close()


# ============================================================================
# RNNOISE INTEGRATION
# ============================================================================

class RNNoiseProcessor:
    """
    RNNoise-based noise suppression
    Requires compiled rnnoise_demo binary
    """
    
    def __init__(self, config: ASRConfig):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self._check_rnnoise_available()
        
    def _check_rnnoise_available(self):
        """Check if RNNoise binary is available"""
        if not os.path.exists(self.config.RNNOISE_PATH):
            self.logger.warning(f"RNNoise not found at {self.config.RNNOISE_PATH}")
            self.config.RNNOISE_ENABLED = False
    
    def process(self, audio: np.ndarray) -> np.ndarray:
        """
        Apply RNNoise noise suppression
        
        RNNoise expects:
        - 16-bit signed PCM
        - 48kHz sample rate
        - Mono channel
        """
        if not self.config.RNNOISE_ENABLED:
            return audio
        
        try:
            # Convert to 48kHz for RNNoise
            audio_48k = self._resample(audio, self.config.SAMPLE_RATE, 48000)
            
            # Convert to 16-bit PCM
            audio_pcm = (audio_48k * 32767).astype(np.int16)
            
            # Create temporary files
            with tempfile.NamedTemporaryFile(suffix='.raw', delete=False) as input_file:
                input_path = input_file.name
                audio_pcm.tofile(input_file)
            
            output_path = input_path + '.out'
            
            try:
                # Run RNNoise
                result = subprocess.run(
                    [self.config.RNNOISE_PATH, input_path, output_path],
                    capture_output=True,
                    timeout=10
                )
                
                if result.returncode != 0:
                    self.logger.error(f"RNNoise failed: {result.stderr}")
                    return audio
                
                # Read processed audio
                processed_audio = np.fromfile(output_path, dtype=np.int16)
                
                # Convert back to float and original sample rate
                processed_audio = processed_audio.astype(np.float32) / 32767.0
                processed_audio = self._resample(processed_audio, 48000, self.config.SAMPLE_RATE)
                
                self.logger.info("RNNoise processing successful")
                return processed_audio
                
            finally:
                # Cleanup temp files
                if os.path.exists(input_path):
                    os.remove(input_path)
                if os.path.exists(output_path):
                    os.remove(output_path)
                    
        except Exception as e:
            self.logger.error(f"RNNoise processing error: {e}")
            return audio
    
    def _resample(self, audio: np.ndarray, orig_sr: int, target_sr: int) -> np.ndarray:
        """Simple resampling"""
        if orig_sr == target_sr:
            return audio
        
        duration = len(audio) / orig_sr
        target_length = int(duration * target_sr)
        indices = np.linspace(0, len(audio) - 1, target_length)
        return np.interp(indices, np.arange(len(audio)), audio)


# ============================================================================
# WEBRTC VAD INTEGRATION
# ============================================================================

class WebRTCVAD:
    """
    WebRTC Voice Activity Detection
    More robust than simple energy-based VAD
    """
    
    def __init__(self, config: ASRConfig):
        self.config = config
        self.vad = webrtcvad.Vad(config.VAD_AGGRESSIVENESS)
        self.logger = logging.getLogger(__name__)
        
    def process(self, audio: np.ndarray) -> np.ndarray:
        """
        Remove silence using WebRTC VAD
        
        WebRTC VAD requires:
        - 16-bit signed PCM
        - 8kHz, 16kHz, or 32kHz sample rate
        - Frame sizes: 10, 20, or 30 ms
        """
        if not self.config.VAD_ENABLED:
            return audio
        
        try:
            # Convert to 16-bit PCM
            audio_pcm = (audio * 32767).astype(np.int16).tobytes()
            
            # Calculate frame size in bytes
            frame_duration_ms = self.config.VAD_FRAME_DURATION_MS
            frame_size = int(self.config.SAMPLE_RATE * frame_duration_ms / 1000)
            frame_bytes = frame_size * 2  # 16-bit = 2 bytes per sample
            
            # Process frames
            voiced_frames = []
            num_frames = len(audio_pcm) // frame_bytes
            
            for i in range(num_frames):
                start = i * frame_bytes
                end = start + frame_bytes
                frame = audio_pcm[start:end]
                
                # Check if frame contains speech
                try:
                    is_speech = self.vad.is_speech(frame, self.config.SAMPLE_RATE)
                    if is_speech:
                        voiced_frames.append(frame)
                except Exception as e:
                    # If VAD fails on frame, include it to be safe
                    voiced_frames.append(frame)
            
            if not voiced_frames:
                self.logger.warning("No speech detected by VAD")
                return audio
            
            # Reconstruct audio from voiced frames
            voiced_audio = b''.join(voiced_frames)
            result = np.frombuffer(voiced_audio, dtype=np.int16).astype(np.float32) / 32767.0
            
            reduction = (1.0 - len(result) / len(audio)) * 100
            self.logger.info(f"VAD removed {reduction:.1f}% of audio (silence)")
            
            return result
            
        except Exception as e:
            self.logger.error(f"WebRTC VAD error: {e}")
            return audio


# ============================================================================
# AUDIO PREPROCESSING (Enhanced)
# ============================================================================

class AudioPreprocessor:
    """
    Enhanced audio preprocessing with RNNoise and WebRTC VAD
    """
    
    def __init__(self, config: ASRConfig):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self.rnnoise = RNNoiseProcessor(config)
        self.webrtc_vad = WebRTCVAD(config)
        
    def load_audio(self, audio_bytes: bytes) -> np.ndarray:
        """Load audio from bytes and convert to numpy array"""
        try:
            with io.BytesIO(audio_bytes) as audio_io:
                with wave.open(audio_io, 'rb') as wav_file:
                    sample_rate = wav_file.getframerate()
                    n_channels = wav_file.getnchannels()
                    n_frames = wav_file.getnframes()
                    
                    audio_data = wav_file.readframes(n_frames)
                    audio_array = np.frombuffer(audio_data, dtype=np.int16)
                    
                    # Convert to mono
                    if n_channels == 2:
                        audio_array = audio_array.reshape(-1, 2).mean(axis=1)
                    
                    # Resample if needed
                    if sample_rate != self.config.SAMPLE_RATE:
                        audio_array = self._resample(audio_array, sample_rate, 
                                                     self.config.SAMPLE_RATE)
                    
                    # Normalize to [-1, 1]
                    audio_array = audio_array.astype(np.float32) / 32768.0
                    
                    return audio_array
                    
        except Exception as e:
            self.logger.error(f"Error loading audio: {e}")
            raise HTTPException(status_code=400, detail="Invalid audio format")
    
    def _resample(self, audio: np.ndarray, orig_sr: int, target_sr: int) -> np.ndarray:
        """Simple resampling"""
        if orig_sr == target_sr:
            return audio
        duration = len(audio) / orig_sr
        target_length = int(duration * target_sr)
        indices = np.linspace(0, len(audio) - 1, target_length)
        return np.interp(indices, np.arange(len(audio)), audio)
    
    def preprocess(self, audio_bytes: bytes) -> np.ndarray:
        """Complete preprocessing pipeline with RNNoise and WebRTC VAD"""
        # Load audio
        audio = self.load_audio(audio_bytes)
        
        # Apply RNNoise for industrial noise suppression
        audio = self.rnnoise.process(audio)
        
        # Apply WebRTC VAD to remove silence
        audio = self.webrtc_vad.process(audio)
        
        return audio


# ============================================================================
# ASR ENGINE
# ============================================================================

class ASREngine:
    """Whisper-based ASR engine"""
    
    def __init__(self, config: ASRConfig):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self.model = None
        self._load_model()
        
    def _load_model(self):
        """Load Whisper model"""
        try:
            self.logger.info(f"Loading Whisper model: {self.config.WHISPER_MODEL}")
            self.model = whisper.load_model(self.config.WHISPER_MODEL, 
                                           device=self.config.DEVICE)
            self.logger.info("Model loaded successfully")
        except Exception as e:
            self.logger.error(f"Failed to load model: {e}")
            raise
    
    def transcribe(self, audio: np.ndarray) -> Dict:
        """Transcribe audio to text"""
        try:
            result = self.model.transcribe(
                audio,
                language=self.config.WHISPER_LANGUAGE,
                task=self.config.WHISPER_TASK,
                fp16=False if self.config.DEVICE == "cpu" else True
            )
            
            # Calculate confidence
            if "segments" in result and result["segments"]:
                avg_confidence = np.mean([
                    seg.get("avg_logprob", 0.0) 
                    for seg in result["segments"]
                ])
                confidence = min(1.0, max(0.0, (avg_confidence + 1.0)))
            else:
                confidence = 0.5
            
            return {
                "text": result["text"].strip(),
                "language": result.get("language", self.config.WHISPER_LANGUAGE),
                "confidence": confidence,
                "segments": result.get("segments", [])
            }
            
        except Exception as e:
            self.logger.error(f"Transcription error: {e}")
            raise


# ============================================================================
# VOCABULARY MANAGER (Database-backed)
# ============================================================================

class VocabularyManager:
    """
    Database-backed vocabulary management with caching
    """
    
    def __init__(self, config: ASRConfig, db_manager: DatabaseManager, 
                 cache_manager: CacheManager):
        self.config = config
        self.db = db_manager
        self.cache = cache_manager
        self.logger = logging.getLogger(__name__)
        self._vocab_cache = {}
        
    async def load_vocabulary(self, vocab_name: str) -> Dict:
        """Load vocabulary from database with caching"""
        # Check memory cache
        if vocab_name in self._vocab_cache:
            return self._vocab_cache[vocab_name]
        
        # Load from database
        terms = await self.db.get_vocabulary_terms(vocab_name)
        
        vocab_data = {
            "terms": terms,
            "loaded_at": datetime.utcnow()
        }
        
        self._vocab_cache[vocab_name] = vocab_data
        return vocab_data
    
    async def match(self, text: str, vocabularies: Optional[List[str]] = None) -> List[SemanticMatch]:
        """Match text against vocabularies"""
        if vocabularies is None:
            vocabularies = self.config.ALLOWED_VOCABULARIES
        
        matches = []
        text_lower = text.lower()
        words = text_lower.split()
        
        for vocab_name in vocabularies:
            try:
                vocab_data = await self.load_vocabulary(vocab_name)
                
                for term_data in vocab_data["terms"]:
                    term = term_data["term"]
                    normalized = term_data["normalized_term"]
                    weight = term_data["confidence_weight"]
                    
                    # Exact match
                    if normalized in text_lower:
                        confidence = 1.0 * weight
                        matches.append(SemanticMatch(
                            matched_term=term,
                            original_text=text,
                            confidence=confidence,
                            vocabulary=vocab_name,
                            metadata=term_data.get("metadata", {})
                        ))
                    # Word match
                    elif any(normalized in word for word in words):
                        confidence = 0.8 * weight
                        matches.append(SemanticMatch(
                            matched_term=term,
                            original_text=text,
                            confidence=confidence,
                            vocabulary=vocab_name,
                            metadata=term_data.get("metadata", {})
                        ))
                    # Synonym match
                    elif term_data.get("synonyms"):
                        for synonym in term_data["synonyms"]:
                            if synonym.lower() in text_lower:
                                confidence = 0.9 * weight
                                matches.append(SemanticMatch(
                                    matched_term=term,
                                    original_text=text,
                                    confidence=confidence,
                                    vocabulary=vocab_name,
                                    metadata=term_data.get("metadata", {})
                                ))
                                break
                
            except Exception as e:
                self.logger.error(f"Error matching vocabulary {vocab_name}: {e}")
                continue
        
        # Sort and deduplicate
        matches.sort(key=lambda x: x.confidence, reverse=True)
        seen = set()
        unique_matches = []
        for match in matches:
            if match.matched_term not in seen:
                seen.add(match.matched_term)
                unique_matches.append(match)
        
        return unique_matches[:self.config.MAX_ALTERNATIVES]
    
    def invalidate_cache(self, vocab_name: Optional[str] = None):
        """Invalidate vocabulary cache"""
        if vocab_name:
            self._vocab_cache.pop(vocab_name, None)
        else:
            self._vocab_cache.clear()


# ============================================================================
# ASR SERVICE (Enhanced)
# ============================================================================

class ASRService:
    """Main ASR service with all integrations"""
    
    def __init__(self, config: ASRConfig, db_manager: DatabaseManager,
                 cache_manager: CacheManager):
        self.config = config
        self.db = db_manager
        self.cache = cache_manager
        self.logger = logging.getLogger(__name__)
        self.preprocessor = AudioPreprocessor(config)
        self.engine = ASREngine(config)
        self.vocabulary = VocabularyManager(config, db_manager, cache_manager)
        
    def _hash_audio(self, audio_bytes: bytes) -> str:
        """Generate SHA256 hash of audio"""
        return hashlib.sha256(audio_bytes).hexdigest()
    
    async def process(self, audio_bytes: bytes, 
                     vocabularies: Optional[List[str]] = None) -> ASRResponse:
        """Complete ASR pipeline with caching"""
        start_time = datetime.utcnow()
        warnings = []
        
        # Calculate audio hash for caching
        audio_hash = self._hash_audio(audio_bytes)
        
        # Check cache
        cached_result = await self.cache.get_cached_result(audio_hash, vocabularies)
        if cached_result:
            return cached_result
        
        try:
            # 1. Preprocess audio (RNNoise + WebRTC VAD)
            audio = self.preprocessor.preprocess(audio_bytes)
            
            if len(audio) == 0:
                result = ASRResponse(
                    status=TranscriptionStatus.ERROR,
                    warnings=["No audio detected after preprocessing"],
                    processing_time_ms=0
                )
                await self.db.log_transcription(audio_hash, result)
                return result
            
            # 2. Transcribe with Whisper
            transcription = self.engine.transcribe(audio)
            
            # 3. Check confidence
            if transcription["confidence"] < self.config.CONFIDENCE_THRESHOLD:
                warnings.append(
                    f"Low confidence: {transcription['confidence']:.2f} < {self.config.CONFIDENCE_THRESHOLD}"
                )
            
            # 4. Semantic matching with database vocabularies
            matches = await self.vocabulary.match(transcription["text"], vocabularies)
            
            # 5. Determine status
            if not matches:
                status = TranscriptionStatus.NO_MATCH
                warnings.append("No vocabulary matches found")
            elif transcription["confidence"] < self.config.CONFIDENCE_THRESHOLD:
                status = TranscriptionStatus.LOW_CONFIDENCE
            else:
                status = TranscriptionStatus.SUCCESS
            
            # 6. Build response
            processing_time = (datetime.utcnow() - start_time).total_seconds() * 1000
            
            result = ASRResponse(
                status=status,
                transcription=TranscriptionResult(
                    text=transcription["text"],
                    confidence=transcription["confidence"],
                    language=transcription["language"],
                    duration=len(audio) / self.config.SAMPLE_RATE
                ),
                semantic_matches=matches,
                alternatives=[m.matched_term for m in matches[1:]],
                processing_time_ms=processing_time,
                warnings=warnings
            )
            
            # 7. Cache result
            await self.cache.cache_result(audio_hash, vocabularies, result)
            
            # 8. Log to database
            matched_vocab = matches[0].vocabulary if matches else None
            await self.db.log_transcription(audio_hash, result, matched_vocab)
            
            return result
            
        except Exception as e:
            self.logger.error(f"ASR processing error: {e}")
            processing_time = (datetime.utcnow() - start_time).total_seconds() * 1000
            result = ASRResponse(
                status=TranscriptionStatus.ERROR,
                warnings=[str(e)],
                processing_time_ms=processing_time
            )
            await self.db.log_transcription(audio_hash, result)
            return result


# ============================================================================
# FASTAPI APPLICATION
# ============================================================================

# Global instances
config = ASRConfig()
db_manager = DatabaseManager(config)
cache_manager = CacheManager(config)
asr_service = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Startup and shutdown events"""
    global asr_service
    
    # Startup
    logging.basicConfig(
        level=getattr(logging, config.LOG_LEVEL.upper()),
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )
    logger = logging.getLogger(__name__)
    
    logger.info("Initializing ASR Service...")
    logger.info(f"Configuration: {json.dumps(config.to_dict(), indent=2)}")
    
    # Initialize database
    await db_manager.initialize()
    
    # Initialize cache
    await cache_manager.initialize()
    
    # Initialize ASR service
    asr_service = ASRService(config, db_manager, cache_manager)
    
    logger.info("ASR Service ready")
    
    yield
    
    # Shutdown
    logger.info("Shutting down ASR Service...")
    await db_manager.close()
    await cache_manager.close()


app = FastAPI(
    title="Industrial ASR Service - Enhanced",
    description="ASR with RNNoise, WebRTC VAD, PostgreSQL, and Redis",
    version="2.0.0",
    lifespan=lifespan
)


@app.get("/")
async def root():
    """Health check"""
    return {
        "service": "ASR Industrial Enhanced",
        "status": "running",
        "config": config.to_dict(),
        "features": {
            "rnnoise": config.RNNOISE_ENABLED,
            "webrtc_vad": config.VAD_ENABLED,
            "database": True,
            "cache": cache_manager.redis is not None
        }
    }


@app.get("/config")
async def get_config():
    """Get current configuration"""
    return config.to_dict()


@app.get("/vocabularies")
async def list_vocabularies():
    """List all vocabularies"""
    async with db_manager.session_maker() as session:
        result = await session.execute("""
            SELECT v.name, v.description, v.category, v.active,
                   COUNT(vt.id) as term_count
            FROM vocabularies v
            LEFT JOIN vocabulary_terms vt ON v.id = vt.vocabulary_id AND vt.active = true
            GROUP BY v.id, v.name, v.description, v.category, v.active
            ORDER BY v.name
        """)
        
        vocabs = []
        for row in result:
            vocabs.append({
                "name": row[0],
                "description": row[1],
                "category": row[2],
                "active": row[3],
                "term_count": row[4]
            })
        
        return {"vocabularies": vocabs}


@app.get("/vocabularies/{vocab_name}/terms")
async def get_vocabulary_terms(vocab_name: str):
    """Get all terms for a vocabulary"""
    terms = await db_manager.get_vocabulary_terms(vocab_name)
    return {
        "vocabulary": vocab_name,
        "terms": terms,
        "count": len(terms)
    }


@app.post("/vocabularies")
async def create_vocabulary(vocab: VocabularyCreate):
    """Create new vocabulary"""
    try:
        vocab_id = await db_manager.add_vocabulary(vocab)
        asr_service.vocabulary.invalidate_cache(vocab.name)
        return {
            "status": "success",
            "vocabulary_id": vocab_id,
            "name": vocab.name
        }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


@app.post("/transcribe", response_model=ASRResponse)
async def transcribe_audio(
    audio: UploadFile = File(...),
    vocabularies: Optional[str] = None
):
    """
    Transcribe audio with enhanced preprocessing
    
    Features:
    - RNNoise industrial noise suppression
    - WebRTC VAD for silence removal
    - Database-backed vocabulary matching
    - Redis caching for performance
    """
    
    # Validate file size
    content = await audio.read()
    if len(content) > config.MAX_AUDIO_SIZE_MB * 1024 * 1024:
        raise HTTPException(
            status_code=413,
            detail=f"File too large. Max: {config.MAX_AUDIO_SIZE_MB}MB"
        )
    
    # Parse vocabularies
    vocab_list = None
    if vocabularies:
        vocab_list = [v.strip() for v in vocabularies.split(",")]
    
    # Process
    result = await asr_service.process(content, vocab_list)
    return result


@app.get("/stats")
async def get_statistics():
    """Get transcription statistics"""
    async with db_manager.session_maker() as session:
        # Total transcriptions
        total = await session.execute("SELECT COUNT(*) FROM transcription_logs")
        total_count = total.scalar()
        
        # By status
        by_status = await session.execute("""
            SELECT status, COUNT(*) as count
            FROM transcription_logs
            GROUP BY status
        """)
        
        status_stats = {row[0]: row[1] for row in by_status}
        
        # Average processing time
        avg_time = await session.execute("""
            SELECT AVG(processing_time_ms) 
            FROM transcription_logs
            WHERE created_at > NOW() - INTERVAL '24 hours'
        """)
        
        avg_processing = avg_time.scalar() or 0
        
        # Top matched vocabularies
        top_vocabs = await session.execute("""
            SELECT matched_vocabulary, COUNT(*) as count
            FROM transcription_logs
            WHERE matched_vocabulary IS NOT NULL
            GROUP BY matched_vocabulary
            ORDER BY count DESC
            LIMIT 5
        """)
        
        top_vocab_stats = [{"vocabulary": row[0], "count": row[1]} for row in top_vocabs]
        
        return {
            "total_transcriptions": total_count,
            "by_status": status_stats,
            "avg_processing_time_ms_24h": round(avg_processing, 2),
            "top_vocabularies": top_vocab_stats
        }


@app.delete("/cache")
async def clear_cache():
    """Clear Redis cache"""
    if cache_manager.redis:
        await cache_manager.redis.flushdb()
        return {"status": "success", "message": "Cache cleared"}
    return {"status": "error", "message": "Cache not available"}


@app.post("/vocabularies/{vocab_name}/invalidate")
async def invalidate_vocabulary_cache(vocab_name: str):
    """Invalidate cache for specific vocabulary"""
    asr_service.vocabulary.invalidate_cache(vocab_name)
    return {"status": "success", "vocabulary": vocab_name}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        app, 
        host=config.API_HOST, 
        port=config.API_PORT, 
        log_level=config.LOG_LEVEL
    )


"""
DEPLOYMENT GUIDE
================

1. Install RNNoise:
   -----------------
   git clone https://github.com/xiph/rnnoise.git
   cd rnnoise
   ./autogen.sh
   ./configure
   make
   sudo make install
   
   # Build demo binary
   gcc -o rnnoise_demo examples/rnnoise_demo.c -lrnnoise -lm
   sudo cp rnnoise_demo /usr/local/bin/

2. Setup PostgreSQL:
   ------------------
   # Create database and user
   sudo -u postgres psql
   CREATE DATABASE asr_db;
   CREATE USER asr_user WITH PASSWORD 'asr_password';
   GRANT ALL PRIVILEGES ON DATABASE asr_db TO asr_user;
   
   # Tables are created automatically on startup

3. Setup Redis:
   -------------
   sudo apt-get install redis-server
   sudo systemctl start redis
   sudo systemctl enable redis

4. Requirements.txt:
   -----------------
   fastapi==0.104.1
   uvicorn[standard]==0.24.0
   openai-whisper==20231117
   torch==2.1.0
   numpy==1.24.3
   pydantic==2.5.0
   webrtcvad==2.0.10
   asyncpg==0.29.0
   SQLAlchemy==2.0.23
   redis==5.0.1
   psycopg2-binary==2.9.9

5. Docker Compose:
   ----------------
   version: '3.8'
   
   services:
     postgres:
       image: postgres:15
       environment:
         POSTGRES_DB: asr_db
         POSTGRES_USER: asr_user
         POSTGRES_PASSWORD: asr_password
       ports:
         - "5432:5432"
       volumes:
         - postgres_data:/var/lib/postgresql/data
     
     redis:
       image: redis:7-alpine
       ports:
         - "6379:6379"
     
     asr-service:
       build: .
       ports:
         - "8000:8000"
       depends_on:
         - postgres
         - redis
       environment:
         DATABASE_URL: postgresql+asyncpg://asr_user:asr_password@postgres:5432/asr_db
         REDIS_URL: redis://redis:6379/0
       volumes:
         - ./models:/root/.cache/whisper
   
   volumes:
     postgres_data:

6. Dockerfile:
   ------------
   FROM python:3.10-slim
   
   # Install system dependencies
   RUN apt-get update && apt-get install -y \\
       ffmpeg libsndfile1 gcc g++ make autoconf automake libtool git && \\
       rm -rf /var/lib/apt/lists/*
   
   # Install RNNoise
   RUN git clone https://github.com/xiph/rnnoise.git && \\
       cd rnnoise && \\
       ./autogen.sh && \\
       ./configure && \\
       make && \\
       make install && \\
       gcc -o /usr/local/bin/rnnoise_demo examples/rnnoise_demo.c -lrnnoise -lm && \\
       cd .. && rm -rf rnnoise
   
   WORKDIR /app
   COPY requirements.txt .
   RUN pip install --no-cache-dir -r requirements.txt
   
   COPY . .
   
   EXPOSE 8000
   CMD ["uvicorn", "asr_service:app", "--host", "0.0.0.0", "--port", "8000"]

7. Testing:
   --------
   # Start services
   docker-compose up -d
   
   # Create vocabulary
   curl -X POST "http://localhost:8000/vocabularies" \\
     -H "Content-Type: application/json" \\
     -d '{
       "name": "custom_parts",
       "description": "Custom parts list",
       "category": "parts",
       "terms": ["motore", "trasmissione", "freno"]
     }'
   
   # Transcribe audio
   curl -X POST "http://localhost:8000/transcribe" \\
     -F "audio=@sample.wav" \\
     -F "vocabularies=custom_parts,quality"
   
   # Check statistics
   curl "http://localhost:8000/stats"

8. Monitoring:
   -----------
   # Check logs
   docker-compose logs -f asr-service
   
   # Redis monitoring
   redis-cli MONITOR
   
   # PostgreSQL queries
   psql -U asr_user -d asr_db -c "SELECT * FROM transcription_logs ORDER BY created_at DESC LIMIT 10;"

9. Performance Tuning:
   -------------------
   - Adjust Whisper model size based on hardware (base/small/medium)
   - Configure Redis maxmemory and eviction policy
   - Tune PostgreSQL connection pool size
   - Adjust VAD aggressiveness for environment
   - Monitor cache hit rates

10. Production Considerations:
    -------------------------
    - Add authentication (JWT)
    - Implement rate limiting
    - Add request queuing
    - Setup backup for PostgreSQL
    - Configure Redis persistence
    - Add health checks
    - Implement circuit breakers
    - Add distributed tracing
    - Setup alerting
"""
