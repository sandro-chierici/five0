using JsonIngestion.DataModels;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace JsonIngestion.Implementation;


// TODO Data Processing
public class DataProcessor
{
    private readonly ILogger<DataProcessor> _logger;
    private readonly IConfiguration _configuration;

    private readonly ActionBlock<InputDataDto> _processData;
    private readonly ActionBlock<InputDataDto> _saveDataFile;
    private readonly BufferBlock<InputDataDto> _inputData;


    public DataProcessor(ILogger<DataProcessor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // define processing pipeline
        _saveDataFile = new ActionBlock<InputDataDto>(SaveDataFile);
        _processData = new ActionBlock<InputDataDto>(ProcessData);
        _inputData = new BufferBlock<InputDataDto>();

        // build parallel pipeline
        _inputData.LinkTo(_saveDataFile);
        _inputData.LinkTo(_processData);
    }

    /// <summary>
    /// Start processing data
    /// </summary>
    /// <param name="data"></param>
    public void EnqueueData(InputDataDto data) => _inputData.Post(data);

    private void SaveDataFile(InputDataDto data)
    {
        //        
        // save raw data to storage
        //
        var path = Path.Combine(
            $@"{_configuration.GetValue<string?>("storagePath") ?? Path.GetTempPath()}",
            $"{data.Request.UserId}",
            "json",
            $"{data.Request.TimestampUTC:yyyyMMddHHmmsszzzz}.json");
        var dirName = Path.GetDirectoryName(path);
        if (dirName is null)
        {
            _logger.LogWarning($"Path {dirName} is impossible to create");
            return;
        }
        Directory.CreateDirectory(dirName);
        try
        {
            using var fs = File.Create(path);
            fs.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(data.Document.RootElement.GetRawText())));

            _logger.LogInformation($"Saved data to {path} for userId {data.Request.UserId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving data to {path} for userId {data.Request.UserId}");
        }
    }

    private void ProcessData(InputDataDto data)
    {



    }
}
