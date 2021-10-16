using GongSolutions.Wpf.DragDrop;
using System.Linq;

namespace bagpipe {
  class ProfileDropHandler : DefaultDropHandler {
    private readonly Profile profile;
    private readonly ProfileViewModel profileVM;
    public ProfileDropHandler(Profile profile, ProfileViewModel profileVM) {
      this.profile = profile;
      this.profileVM = profileVM;
    }

    public override void Drop(IDropInfo dropInfo) {
      if (dropInfo?.DragInfo == null) {
        return;
      }

      int insertIndex = GetInsertIndex(dropInfo);
      IOrderedEnumerable<ProfileEntryViewModel> selectedItems = (
        ExtractData(dropInfo.Data)
        .Cast<ProfileEntryViewModel>()
        .OrderBy(entryVM => profileVM.Entries.IndexOf(entryVM))
      );

      foreach (ProfileEntryViewModel entryVM in selectedItems) {
        int index = profileVM.Entries.IndexOf(entryVM);
        if (insertIndex > index) {
          insertIndex--;
        }

        profile.Entries.Move(index, insertIndex);
        insertIndex++;
      }
    }
  }
}
