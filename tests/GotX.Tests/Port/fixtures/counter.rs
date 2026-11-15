use gpui::*;

struct Counter { count: usize, active: bool, focus: FocusHandle }

impl Render for Counter {
    fn render(&mut self, window: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
        h_flex()
            .id("row")
            .size_full()
            .gap_0p5()
            .px_4()
            .w(px(320.))
            .min_w(rems(2.))
            .w_1_2()
            .rounded_lg()
            .border_t_1()
            .border_color(rgb(0xff0000))
            .bg(cx.theme().colors().elevated_surface_background)
            .text_color(hsla(0.0, 1.0, 0.5, 0.5))
            .font_weight(FontWeight::SEMIBOLD)
            .overflow_x_scroll()
            .hover(|s| s.bg(rgb(0x333333)))
            .when(self.active, |this| this.opacity(0.5).flex_col())
            .z_index(3)
            .on_click(cx.listener(|this, _, _, cx| { this.count += 1; cx.notify(); }))
            .on_key_down(cx.listener(|this, ev, _, cx| {}))
            .child(div().mx_auto().child("x"))
            .child(uniform_list(cx.entity(), "rows", 10, |this, range, _w, _cx| Vec::new()).flex_1())
    }
}
