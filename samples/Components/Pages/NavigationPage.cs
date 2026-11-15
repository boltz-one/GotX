using GotX;
using GotX.Components;

/// <summary>Trang gallery nhóm Navigation: điều hướng, bố cục và dữ liệu.</summary>
public sealed class NavigationPage : View
{
    Accordion _accordion = null!, _accordionMulti = null!;
    Collapsible _collapsible = null!;
    Tabs _tabs = null!, _tabsLine = null!, _tabsVertical = null!;
    Chart _lineChart = null!, _areaChart = null!;
    Pagination _pagination = null!;
    Resizable _resizeH = null!, _resizeV = null!;
    Carousel _carousel = null!;
    Sidebar _sidebar = null!;

    protected override void OnCreated()
    {
        _accordion = App.New(new Accordion { Items = NavigationDemos.FaqItems });
        _accordion.SetOpen(0, true);
        _accordionMulti = App.New(new Accordion { Type = AccordionType.Multiple, Items = NavigationDemos.FaqItems });
        _accordionMulti.SetOpen(1, true);
        _accordionMulti.SetOpen(2, true);
        _collapsible = App.New(new Collapsible { Trigger = NavigationDemos.RepoHeader, Content = NavigationDemos.RepoList });
        _tabs = App.New(new Tabs { Items = ["Tài khoản", "Mật khẩu", "Thông báo"], Content = NavigationDemos.TabContent });
        _tabsLine = App.New(new Tabs { Items = ["Tổng quan", "Phân tích", "Báo cáo"], Variant = TabsVariant.Line, Content = NavigationDemos.TabContent });
        _tabsVertical = App.New(new Tabs { Items = ["Chung", "Bảo mật", "Thanh toán"], Orientation = TabsOrientation.Vertical, Content = NavigationDemos.TabContent });
        _lineChart = App.New(NavigationDemos.NewChart(ChartKind.Line));
        _areaChart = App.New(NavigationDemos.NewChart(ChartKind.Area));
        _pagination = App.New(new Pagination { PageCount = 10 });
        _pagination.Page = 4;
        App.Subscribe<Pagination.PageChanged>(_pagination, _ => Notify());
        _resizeH = App.New(new Resizable { First = NavigationDemos.Panel("Một"), Second = NavigationDemos.Panel("Hai"), WithHandle = true, BoxClass = "h-40", FirstMinSize = 0.2f, SecondMinSize = 0.2f });
        _resizeV = App.New(new Resizable
        {
            Direction = ResizableDirection.Vertical, First = NavigationDemos.Panel("Trên"), Second = NavigationDemos.Panel("Dưới"), Size = 0.35f, BoxClass = "h-40",
        });
        _carousel = App.New(new Carousel { Count = 5, Slide = NavigationDemos.Slide });
        _sidebar = App.New(new Sidebar { Groups = NavigationDemos.SidebarGroups, Header = NavigationDemos.SidebarHeader, Footer = NavigationDemos.SidebarFooter });
        _sidebar.ActiveIndex = 0;
    }

    public override Div Render(Window w)
    {
        var page = Demo.Page(w);
        page.Child(w.Div().Class("grid grid-cols-2 gap-4")
            .Child(Demo.Section(w, "Accordion", "Single (mở một mục đóng mục khác) và Multiple.")
                .Child(w.View(_accordion))
                .Child(w.Div().Class("h-px bg-border"))
                .Child(w.View(_accordionMulti)))
            .Child(w.Div().Class("flex flex-col gap-4")
                .Child(Demo.Section(w, "Collapsible", "Bấm phần đầu để mở/đóng.").Child(w.View(_collapsible)))
                .Child(Demo.Section(w, "Breadcrumb", "Link, dấu phân cách, dấu … và trang hiện tại.").Child(NavigationDemos.Breadcrumb(w)))
                .Child(Demo.Section(w, "Pagination", "Trang 1 thì nút Trước bị vô hiệu; dấu … thay cho các trang bị ẩn.")
                    .Child(w.View(_pagination))
                    .Child(w.Div().Class("text-xs text-muted-foreground text-center").Child(NavigationDemos.PageLabel(_pagination.Page))))));

        page.Child(w.Div().Class("grid grid-cols-2 gap-4")
            .Child(Demo.Section(w, "Tabs", "Variant default, line và hướng dọc; ←/→ (dọc: ↑/↓) đổi tab.").Child(w.View(_tabs)).Child(w.View(_tabsLine)).Child(w.View(_tabsVertical)))
            .Child(Demo.Section(w, "Carousel", "Nút lùi/tiến bị vô hiệu ở hai đầu; focus vào nút rồi ←/→.")
                .Child(w.Div().Class("px-12").Child(w.View(_carousel)))));

        page.Child(Demo.Section(w, "Table", "Header, body, footer, caption; cột số căn phải.").Child(NavigationDemos.Table(w)));

        page.Child(w.Div().Class("grid grid-cols-2 gap-4")
            .Child(Demo.Section(w, "Scroll area", "Cuộn dọc và cuộn ngang.").Child(NavigationDemos.ScrollAreas(w)))
            .Child(Demo.Section(w, "Resizable", "Kéo thanh chia hoặc Tab tới nó rồi dùng mũi tên; mỗi panel có kích thước tối thiểu.")
                .Child(w.Div().Class("grid grid-cols-2 gap-3")
                    .Child(w.Div().Class("rounded-lg border border-border overflow-hidden").Child(w.View(_resizeH)))
                    .Child(w.Div().Class("rounded-lg border border-border overflow-hidden").Child(w.View(_resizeV))))));

        page.Child(Demo.Section(w, "Chart", "Cột, đường và vùng trên cùng một khung, dùng token chart-1..5.").Child(NavigationDemos.Chart(w))
            .Child(w.Div().Class("grid grid-cols-2 gap-6").Child(w.View(_lineChart)).Child(w.View(_areaChart))));

        page.Child(Demo.Section(w, "Sidebar", "Nút ở góc trên vùng nội dung thu gọn sidebar còn cột icon.")
            .Child(w.Div().Class("flex h-96 rounded-lg border border-border overflow-hidden")
                .Child(w.View(_sidebar))
                .Child(Sidebar.Inset(w)
                    .Child(w.Div().Class("flex h-12 shrink-0 items-center gap-2 border-b border-border px-3")
                        .Child(_sidebar.Trigger(w))
                        .Child(w.Div().Class("h-4 w-px bg-border"))
                        .Child(w.Div().Class("text-sm font-medium").Child("Bảng điều khiển")))
                    .Child(NavigationDemos.InsetBody(w)))));
        return page;
    }
}
