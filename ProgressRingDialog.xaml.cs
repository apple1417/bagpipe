using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace bagpipe {
  public partial class ProgressRingDialog : CustomDialog {
    public ProgressRingDialog() : this(null, null) {}
    public ProgressRingDialog(MetroWindow parentWindow) : this(parentWindow, null) { }
    public ProgressRingDialog(MetroDialogSettings settings) : this(null, settings) { }
    public ProgressRingDialog(MetroWindow parentWindow, MetroDialogSettings settings) : base(parentWindow, settings) {
      InitializeComponent();
    }
  }
}
