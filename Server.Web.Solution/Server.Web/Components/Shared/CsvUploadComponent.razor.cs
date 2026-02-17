
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ServerBB_Web.Components.Shared
{
    public partial class CsvUploadComponent
    {
        [Parameter] public string Label { get; set; } = "Upload CSV";
        [Parameter] public bool Multiple { get; set; }

        [Parameter]
        public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

        private List<string> _fileNames = new();

        private async Task HandleFilesSelected(InputFileChangeEventArgs e)
        {
            var files = Multiple
                ? e.GetMultipleFiles()
                : new List<IBrowserFile> { e.File };

            _fileNames = files.Select(f => f.Name).ToList();

            await OnFilesSelected.InvokeAsync(files);
        }
    }
}
