using System;
using System.Reflection;
using System.Runtime.Serialization;
using OracleOfDereth;
using VirindiViewService;
using VirindiViewService.Controls;

internal static class HudImageReuseTests
{
    public static void Run()
    {
        const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        Type mainView = typeof(ItemListRenderer).Assembly.GetType("OracleOfDereth.MainView", true);
        object view = Empty(mainView);
        MethodInfo mainAssign = mainView.GetMethod("AssignImage", hidden, null,
            new[] { typeof(HudPictureBox), typeof(int) }, null);
        MethodInfo itemAssign = typeof(ItemListRenderer).GetMethod("AssignImage", BindingFlags.NonPublic | BindingFlags.Static);

        foreach (MethodInfo assign in new[] { mainAssign, itemAssign })
        {
            object target = assign.IsStatic ? null : view;
            foreach (int shortId in new[] { 4887, 4819, 9046 })
            {
                // Seed the state of an already-rendered portal image without loading AC's
                // DAT files or creating a Direct3D device. These fields are from the bundled
                // VVS assembly: its int constructor stores icon | 0x06000000 in field d.
                var image = (ACImage)Empty(typeof(ACImage));
                typeof(ACImage).GetField("d", hidden).SetValue(image, shortId | 0x06000000);
                typeof(ACImage).GetField("i", hidden).SetValue(image, true);
                var box = (HudPictureBox)Empty(typeof(HudPictureBox));
                typeof(HudPictureBox).GetField("a", hidden).SetValue(box, image);
                if (image.PortalImageID != (shortId | 0x06000000))
                    throw new InvalidOperationException("The VVS image fixture no longer matches the bundled library.");

                for (int repaint = 0; repaint < 100; repaint++)
                {
                    int requested = repaint % 2 == 0 ? shortId : shortId | 0x06000000;
                    assign.Invoke(target, new object[] { box, requested });
                    if (!ReferenceEquals(box.Image, image))
                        throw new InvalidOperationException("An unchanged HUD icon was replaced during repaint.");
                }
            }

            // Zero must remain an empty image, not become portal resource 0x06000000.
            var emptyBox = (HudPictureBox)Empty(typeof(HudPictureBox));
            assign.Invoke(target, new object[] { emptyBox, 0 });
            if (emptyBox.Image != null)
                throw new InvalidOperationException("An empty HUD icon was populated during repaint.");
        }

        MethodInfo setEnabled = mainView.GetMethod("SetFellowshipButtonEnabled", BindingFlags.NonPublic | BindingFlags.Static);
        var first = (HudButton)Empty(typeof(HudButton));
        var second = (HudButton)Empty(typeof(HudButton));
        try
        {
            setEnabled.Invoke(null, new object[] { first, false });
            setEnabled.Invoke(null, new object[] { second, false });
            ACImage firstImage = first.Image, secondImage = second.Image;
            if (firstImage == null || secondImage == null || ReferenceEquals(firstImage, secondImage))
                throw new InvalidOperationException("Fellowship buttons must own separate disabled overlays.");
            for (int repaint = 0; repaint < 100; repaint++)
                setEnabled.Invoke(null, new object[] { first, false });
            if (!ReferenceEquals(first.Image, firstImage))
                throw new InvalidOperationException("An unchanged button overlay was replaced during repaint.");
            setEnabled.Invoke(null, new object[] { first, true });
            if (first.Image != null || !ReferenceEquals(second.Image, secondImage))
                throw new InvalidOperationException("Enabling one button changed another button's overlay.");
            setEnabled.Invoke(null, new object[] { first, false });
            if (first.Image == null || ReferenceEquals(first.Image, firstImage))
                throw new InvalidOperationException("A button reused its disposed overlay.");
        }
        finally
        {
            first.Image = null;
            second.Image = null;
        }
    }

    private static object Empty(Type type)
    {
        object value = FormatterServices.GetUninitializedObject(type);
        GC.SuppressFinalize(value); // The fixtures never acquired native resources.
        return value;
    }
}
