using System.ComponentModel.DataAnnotations;

namespace Feast.Aspire.DbService;

public class SampleEntity
{
    [Key]
    public int Id { get;                   set; }
    public string?        Name      { get; set; }
    public Guid           DeviceId  { get; set; }
    public double         Credit    { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}