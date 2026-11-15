using GotX;
using GotX.Components;

static partial class HomeCards
{
    /// <summary>claimable-balance.tsx: số dư có thể rút, bảng tính phí, ghi chú điều kiện chi trả.</summary>
    public static Div ClaimableBalance(Window w) =>
        HomeUi.Card(w).Key("claimable-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardDescription(w, "Claimable Balance"))
                .Child(Ui.CardTitle(w, "$1,211.29").Class("text-4xl tabular-nums"))
                // Header là grid nên con bị kéo giãn hết hàng; bọc flex để badge co theo chữ (w-fit của bản gốc).
                .Child(w.Div().Class("flex").Child(Ui.Badge(w, BadgeVariant.Outline)
                    .Child(w.Div().Class("size-2 rounded-full bg-yellow-500"))
                    .Child("Pending Setup"))))
            .Child(HomeUi.Content(w).Class("flex flex-1 flex-col justify-end").Child(MutedColumn(w).Child(w.Div().Class("flex flex-col gap-3")
                .Child(Row(w, "Net Royalties", "$1,248.75", "text-sm font-medium tabular-nums"))
                .Child(Row(w, "Processing Fee", "-$37.46", "text-sm font-medium tabular-nums"))
                .Child(Ui.Separator(w))
                .Child(Row(w, "Total Ready to Claim", "$1,211.29 USD", "text-sm font-semibold tabular-nums")))))
            .Child(HomeUi.Footer(w).Child(Ui.CardDescription(w,
                "Once your bank is connected, balances over $10.00 are automatically eligible for monthly distribution on the 15th of each month.")));

    // flex items-center justify-between, không có gap như bản gốc.
    static Div Row(Window w, string label, string value, string valueClass) =>
        w.Div().Class("flex items-center justify-between")
            .Child(w.Div().Class("text-sm text-muted-foreground").Child(label))
            .Child(w.Div().Class(valueClass).Child(value));
}