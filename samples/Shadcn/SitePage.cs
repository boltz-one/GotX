using GotX;
using GotX.Components;

/// <summary>
/// Một trang của site ví dụ. <paramref name="Create"/> dựng view của trang (gọi một lần, khi trang được mở lần đầu).
/// <paramref name="FullBleed"/> = trang tự lo toàn bộ khung nhìn (block dashboard/sidebar/login); ngược lại shell cho trang cuộn dọc.
/// <paramref name="Style"/> = style component của trang: shell bật nó khi mở trang (các block của shadcn dùng NewYorkV4,
/// trang chủ tự ghim style riêng nên để Default), và pipeline chụp ảnh dùng cùng giá trị.
/// </summary>
public readonly record struct SitePage(string Id, string Title, Func<App, View> Create, bool FullBleed = false, ComponentStyle Style = ComponentStyle.Default);