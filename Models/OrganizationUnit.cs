namespace HcPortal.Models
{
    public class OrganizationUnit
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ParentId { get; set; }
        public int Level { get; set; } = 0;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public OrganizationUnit? Parent { get; set; }
        public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();
        public ICollection<KkjFile> KkjFiles { get; set; } = new List<KkjFile>();
        public ICollection<CpdpFile> CpdpFiles { get; set; } = new List<CpdpFile>();
    }
}
