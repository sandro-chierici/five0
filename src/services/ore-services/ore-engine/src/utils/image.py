"""Image processing utilities"""

import cv2
import numpy as np
from PIL import Image
from io import BytesIO


def decode_image(data: bytes) -> np.ndarray:
    """
    Decode image bytes to numpy array
    
    Args:
        data: Image bytes (JPEG, PNG, etc.)
    
    Returns:
        Image as numpy array (BGR format)
    """
    # Decode with OpenCV
    nparr = np.frombuffer(data, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    
    if img is None:
        raise ValueError("Failed to decode image")
    
    return img


def encode_image(img: np.ndarray, format: str = "jpg", quality: int = 95) -> bytes:
    """
    Encode numpy array to image bytes
    
    Args:
        img: Image as numpy array
        format: Image format (jpg, png)
        quality: JPEG quality (0-100)
    
    Returns:
        Encoded image bytes
    """
    if format.lower() in ["jpg", "jpeg"]:
        encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), quality]
        _, buffer = cv2.imencode(".jpg", img, encode_param)
    elif format.lower() == "png":
        _, buffer = cv2.imencode(".png", img)
    else:
        raise ValueError(f"Unsupported format: {format}")
    
    return buffer.tobytes()


def resize_image(img: np.ndarray, target_size: tuple) -> np.ndarray:
    """
    Resize image to target size
    
    Args:
        img: Input image
        target_size: (width, height)
    
    Returns:
        Resized image
    """
    return cv2.resize(img, target_size, interpolation=cv2.INTER_LINEAR)


def preprocess_for_yolo(img: np.ndarray, input_size: int = 640) -> np.ndarray:
    """
    Preprocess image for YOLO inference
    
    Args:
        img: Input image (BGR)
        input_size: Model input size
    
    Returns:
        Preprocessed image
    """
    # Convert BGR to RGB
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    
    # Resize while maintaining aspect ratio
    h, w = img_rgb.shape[:2]
    scale = input_size / max(h, w)
    new_h, new_w = int(h * scale), int(w * scale)
    img_resized = cv2.resize(img_rgb, (new_w, new_h))
    
    # Pad to square
    canvas = np.full((input_size, input_size, 3), 114, dtype=np.uint8)
    y_offset = (input_size - new_h) // 2
    x_offset = (input_size - new_w) // 2
    canvas[y_offset:y_offset+new_h, x_offset:x_offset+new_w] = img_resized
    
    return canvas


def draw_detections(
    img: np.ndarray,
    detections: list,
    class_names: dict = None,
    conf_threshold: float = 0.5
) -> np.ndarray:
    """
    Draw bounding boxes on image
    
    Args:
        img: Input image
        detections: List of detection dictionaries
        class_names: Mapping of class IDs to names
        conf_threshold: Confidence threshold
    
    Returns:
        Image with drawn detections
    """
    img_copy = img.copy()
    h, w = img.shape[:2]
    
    for det in detections:
        if det['confidence'] < conf_threshold:
            continue
        
        # Denormalize bbox coordinates
        x = int(det['bbox']['x'] * w)
        y = int(det['bbox']['y'] * h)
        box_w = int(det['bbox']['width'] * w)
        box_h = int(det['bbox']['height'] * h)
        
        # Draw rectangle
        cv2.rectangle(img_copy, (x, y), (x + box_w, y + box_h), (0, 255, 0), 2)
        
        # Draw label
        label = f"{det['class_name']}: {det['confidence']:.2f}"
        cv2.putText(
            img_copy, label, (x, y - 10),
            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2
        )
    
    return img_copy
