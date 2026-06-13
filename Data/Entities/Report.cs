namespace BocceManager.Data.Entities;

public class Report
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ReportPath { get; set; } = "";
    public string Description { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

public class ReportParameter
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string ParameterName { get; set; } = "";
    public string ParameterLabel { get; set; } = "";
    public bool IsRequired { get; set; }
    public string DefaultSource { get; set; } = "";
    public int DisplayOrder { get; set; }

    public Report Report { get; set; } = null!;
}
