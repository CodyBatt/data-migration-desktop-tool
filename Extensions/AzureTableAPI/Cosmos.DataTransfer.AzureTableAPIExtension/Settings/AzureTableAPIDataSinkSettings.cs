namespace Cosmos.DataTransfer.AzureTableAPIExtension.Settings
{
    public class AzureTableAPIDataSinkSettings : AzureTableAPISettingsBase
    {
        /// <summary>
        /// Indicates whether to overwrite existing entries
        /// </summary>
        public bool Overwrite { get; set; }

        /// <summary>
        /// Size of transaction batch
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}