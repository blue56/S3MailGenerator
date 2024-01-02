using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MimeKit;
using Newtonsoft.Json.Linq;

namespace S3MailGenerator;

public class EmlFileGenerator
{
    private IAmazonS3 _s3Client;

    private RegionEndpoint _region = null;

    public async Task Generate(string Region, string Bucketname,
        string JsonFile, string ResultFile)
    {
        // Make S3 client

        // Set the AWS region where your S3 bucket is located
        _region = RegionEndpoint.GetBySystemName(Region);

        // Create an S3 client
        _s3Client = new AmazonS3Client(_region);

        // Read JSON file content
        string content = GetFileContentFromS3(Bucketname, JsonFile).Result;

        // Parse JSON
        JObject json = JObject.Parse(content);

        // Make eml file in S3
        var message = new MimeMessage();

        string fromAddress = (string)json["From"];
        string fromAddressName = (string)json["FromName"];

        if (fromAddressName == null) {
            fromAddressName = fromAddress;
        }

        message.From.Add(new MailboxAddress(fromAddressName, fromAddressName));

        string toAddress = (string)json["To"];
        string toAddressName = (string)json["ToName"];

        if (toAddressName == null) {
            toAddressName = toAddress;
        }

        message.To.Add(new MailboxAddress(toAddress, toAddress));

        string subject = (string)json["Subject"];

        message.Subject = subject;

        string body = (string)json["Body"];

        if (body == null)
        {
            // Include body get content from external file
            string includeBody = (string)json["BodyS3Path"];

            if (includeBody.StartsWith("./")) {
                int i = JsonFile.LastIndexOf("/");
                string prefix = JsonFile.Substring(0, i+1);

                includeBody = includeBody.Replace("./", prefix);
            }

            // Read content from file
            body = GetFileContentFromS3(Bucketname, includeBody).Result;
        }

        message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = body
        };

        // Write result file
        MemoryStream ms = new MemoryStream();
        message.WriteTo(ms);

        SaveFile(_s3Client, Bucketname, ResultFile, 
            ms, "application/octet-stream");
    }

    public void SaveFile(IAmazonS3 _s3Client, string Bucketname, 
        string S3Path, Stream Stream, string ContentType)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = Bucketname,
            Key = S3Path,
            ContentType = ContentType,
            InputStream = Stream
        };

        _s3Client.PutObjectAsync(putRequest).Wait();
    }

    private async Task<string> GetFileContentFromS3(string bucketName, string key)
    {
        try
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
            using (Stream responseStream = response.ResponseStream)
            using (StreamReader reader = new StreamReader(responseStream))
            {
                return await reader.ReadToEndAsync();
            }
        }
        catch (AmazonS3Exception e)
        {
            // Handle S3 exception
            return $"Error getting template: {e.Message}";
        }
    }
}