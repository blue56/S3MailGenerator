namespace S3MailGenerator;

public class MailGeneratorRequest
{
    public string Region { get; set; }
    public string Bucketname { get; set; }
    public string Source {get; set;}
    public string Result { get; set; }
}