namespace ResourceManager.Business.DataModel
{
    /// <summary>
    /// Resources hierarchy
    /// </summary>
    public class ResourceHierarchy
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public long ChildId { get; set; }
        public ResourceHierarchy() { }
    }
}
