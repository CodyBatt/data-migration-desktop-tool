using System.ComponentModel.Composition;
using Azure.Data.Tables;
using Cosmos.DataTransfer.AzureTableAPIExtension.Data;
using Cosmos.DataTransfer.AzureTableAPIExtension.Settings;
using Cosmos.DataTransfer.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cosmos.DataTransfer.AzureTableAPIExtension
{
    [Export(typeof(IDataSinkExtension))]
    public class AzureTableAPIDataSinkExtension : IDataSinkExtensionWithSettings
    {
        public string DisplayName => "AzureTableAPI";

        public async Task WriteAsync(IAsyncEnumerable<IDataItem> dataItems, IConfiguration config, IDataSourceExtension dataSource, ILogger logger, CancellationToken cancellationToken = default)
        {
            var settings = config.Get<AzureTableAPIDataSinkSettings>();
            settings.Validate();

            var serviceClient = new TableServiceClient(settings.ConnectionString);
            var tableClient = serviceClient.GetTableClient(settings.Table);
            await tableClient.CreateIfNotExistsAsync(cancellationToken);

            var batchSize = settings.BatchSize;
            int batchNumber = 1;
            int entityTotal = 0;
            var transactionActions = new List<TableTransactionAction>();
            await foreach(var item in dataItems.WithCancellation(cancellationToken))
            {
                var tableEntity = item.ToTableEntity(settings.PartitionKeyFieldName, settings.RowKeyFieldName);
                transactionActions.Add(settings.Overwrite
                    ? new TableTransactionAction(TableTransactionActionType.Add, tableEntity)
                    : new TableTransactionAction(TableTransactionActionType.UpdateReplace, tableEntity));
                entityTotal++;

                if (transactionActions.Count == batchSize)
                {
                    Console.Write($"Processing batch: {batchNumber++} (Size: {batchSize})\r");
                    await tableClient.SubmitTransactionAsync(transactionActions, cancellationToken);
                    transactionActions = new List<TableTransactionAction>();
                }
            }

            // Process remainder
            Console.WriteLine($"Processing remainder: {transactionActions.Count}                                                   ");
            await tableClient.SubmitTransactionAsync(transactionActions, cancellationToken);
            Console.WriteLine($"Entities processed: {entityTotal}");
        }

        public IEnumerable<IDataExtensionSettings> GetSettings()
        {
            yield return new AzureTableAPIDataSinkSettings();
        }
    }
}
