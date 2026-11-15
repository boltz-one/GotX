using GotX;
using GotX.Components;

static partial class HomeCards
{
    /// <summary>empty-distribute-track.tsx: trạng thái rỗng với nút tạo bản phát hành.</summary>
    public static Div DistributeTrack(Window w) =>
        // Ui.Empty có flex-1: cha phải là cột flex, trong cha block nó co về cao 0.
        HomeUi.Card(w).Key("distribute-card").Child(HomeUi.Content(w).Class("flex flex-col").Child(Ui.Empty(w).Class("p-4")
            .Child(Ui.EmptyMedia(w, EmptyMediaVariant.Icon).Child(HugeIcons.Add01(w).Class("size-5")))
            .Child(Ui.EmptyHeader(w)
                .Child(Ui.EmptyTitle(w, "Distribute Track"))
                .Child(Ui.EmptyDescription(w, "Upload your first master to start reaching listeners on Spotify, Apple Music, and more.")))
            .Child(Ui.EmptyContent(w).Child(Ui.Button(w, "Create Release").Key("create-release")))));
}
