
namespace IPCSoftware.UI.CommonViews.ViewModels
{
    internal class CommonOpenFileDialog
    {
        public bool IsFolderPicker { get; set; }
        public string Title { get; set; }
        public bool AllowNonFileSystemItems { get; set; }
        public bool Multiselect { get; set; }
        public string FileName { get; internal set; }

        internal object ShowDialog()
        {
            throw new NotImplementedException();
        }
    }
}