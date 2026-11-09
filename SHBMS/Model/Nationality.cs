namespace SHBMS.Model;

public partial class Nationality
{
    public string NationalityId { get; set; } = null!;

    public string NationalityName { get; set; } = null!;

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
