using Amazon.DynamoDBv2.DataModel;

namespace DynamoStudentManager.Models
{

    // Create table first:
    //   aws dynamodb create-table --table-name students --attribute-definitions AttributeName=id,AttributeType=N --key-schema AttributeName=id,KeyType=HASH --provisioned-throughput ReadCapacityUnits=1,WriteCapacityUnits=1 --endpoint-url http://dynamodb-1.lucasnet.int:8000

    [DynamoDBTable("students")]
    public class Student
    {
        [DynamoDBHashKey("id")]
        public int? Id { get; set; }

        [DynamoDBProperty("first_name")]
        public string? FirstName { get; set; }

        [DynamoDBProperty("last_name")]
        public string? LastName { get; set; }

        [DynamoDBProperty("college")]
        public string? College { get; set; }

        [DynamoDBProperty("class")]
        public int Class { get; set; }

        [DynamoDBProperty("state")]
        public string? State { get; set; }

        [DynamoDBProperty("country")]
        public string? Country { get; set; }

        [DynamoDBProperty("created")]
        public DateTime? Created { get; set; }

        [DynamoDBProperty("updated")]
        public DateTime? Updated { get; set; }

    }
}