namespace ResourceManager.Business.DataModel
{
    /// <summary>
    /// Resources hierarchy
    /// </summary>
    public class ResourceTypeHierarchy
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int ChildId { get; set; }
        public ResourceTypeHierarchy() { }
    }
}
