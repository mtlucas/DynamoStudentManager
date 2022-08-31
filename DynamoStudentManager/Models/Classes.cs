using Amazon.DynamoDBv2.DataModel;

namespace DynamoStudentManager.Models
{

    // Create table first:
    //   aws dynamodb create-table --table-name classes --attribute-definitions AttributeName=id,AttributeType=N --key-schema AttributeName=id,KeyType=HASH --provisioned-throughput ReadCapacityUnits=1,WriteCapacityUnits=1 --endpoint-url http://dynamodb-1.lucasnet.int:8000

    [DynamoDBTable("classes")]
    public class Classes
    {
        [DynamoDBHashKey("id")]
        public int? Id { get; set; }

        [DynamoDBProperty("name")]
        public string? Name { get; set; }

        [DynamoDBProperty("credits")]
        public int Credits { get; set; }

    }
}