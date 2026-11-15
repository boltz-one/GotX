namespace GotX.Port;

/// <summary>Built-in sample for <c>gpui --sample</c>: a typical GPUI view (hand-written in GPUI style, not copied from Zed).</summary>
public static class GpuiSample
{
    public const string Source = """
        use gpui::*;

        actions!(sample, [Increment]);

        struct Counter {
            count: usize,
            active: bool,
            focus: FocusHandle,
        }

        impl EventEmitter<Changed> for Counter {}

        impl Render for Counter {
            fn render(&mut self, window: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
                let colors = cx.theme().colors();
                v_flex()
                    .id("counter")
                    .size_full()
                    .gap_2()
                    .p_4()
                    .bg(rgb(0x1e1e1e))
                    .child(
                        div()
                            .flex()
                            .items_center()
                            .justify_between()
                            .px_3()
                            .py_1p5()
                            .w_1_2()
                            .h(px(32.))
                            .rounded_md()
                            .border_1()
                            .border_color(cx.theme().colors().border)
                            .bg(cx.theme().colors().surface_background)
                            .text_color(hsla(0.6, 0.5, 0.5, 0.8))
                            .text_sm()
                            .font_weight(FontWeight::BOLD)
                            .cursor_pointer()
                            .overflow_y_scroll()
                            .hover(|s| s.bg(rgba(0xffffff1a)).text_color(white()))
                            .when(self.active, |this| this.bg(cx.theme().colors().element_selected).opacity(0.5))
                            .on_click(cx.listener(|this, _ev, _window, cx| {
                                this.count += 1;
                                cx.emit(Changed);
                                cx.notify();
                            }))
                            .track_focus(&self.focus)
                            .child(format!("Count: {}", self.count)),
                    )
                    .child(
                        uniform_list(cx.entity(), "rows", 100, |this, range, _w, _cx| {
                            range.map(|i| div().id(i).mx_auto().child(i.to_string())).collect()
                        })
                        .flex_1(),
                    )
                    .child(Button::new("go", "Go").on_click(cx.listener(|this, _, _, cx| cx.notify())))
            }
        }
        """;
}
