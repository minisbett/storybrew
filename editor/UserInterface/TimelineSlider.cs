﻿using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.Graphics;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface.Drawables;
using StorybrewEditor.Util;
using System;

namespace StorybrewEditor.UserInterface
{
    public class TimelineSlider : Slider
    {
        private static readonly Color4 tickBlue = new Color4(50, 128, 255, 225);
        private static readonly Color4 tickYellow = new Color4(255, 255, 0, 225);
        private static readonly Color4 tickRed = new Color4(255, 0, 0, 225);
        private static readonly Color4 tickViolet = new Color4(200, 0, 200, 225);
        private static readonly Color4 tickWhite = new Color4(255, 255, 255, 220);
        private static readonly Color4 tickMagenta = new Color4(144, 64, 144, 225);
        private static readonly Color4 tickGrey = new Color4(160, 160, 160, 225);

        private Project project;
        private Sprite line;

        public int SnapDivisor = 4;

        public TimelineSlider(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;
            line = new Sprite()
            {
                Texture = DrawState.WhitePixel,
                ScaleMode = ScaleMode.Fill,
            };
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);

            var bounds = Bounds;
            var offset = new Vector2(bounds.Left, bounds.Top);
            var lineBottomY = bounds.Height * 0.6f;

            var timeSpan = SnapDivisor >= 2 ? SnapDivisor >= 8 ? 1 : 2 : 4;
            var leftTime = (int)((Value - timeSpan) * 1000);
            var rightTime = (int)((Value + timeSpan) * 1000);
            var timeScale = bounds.Width / (rightTime - leftTime);

            // Ticks
            var leftTimingPoint = project.MainBeatmap.GetTimingPointAt(leftTime);
            var timingPoints = project.MainBeatmap.TimingPoints.GetEnumerator();

            if (timingPoints.MoveNext())
            {
                var timingPoint = timingPoints.Current;
                while (timingPoint != null)
                {
                    var nextTimingPoint = timingPoints.MoveNext() ? timingPoints.Current : null;
                    if (timingPoint.Offset < leftTimingPoint.Offset)
                    {
                        timingPoint = nextTimingPoint;
                        continue;
                    }
                    if (timingPoint != leftTimingPoint && rightTime < timingPoint.Offset) break;

                    int tickCount = 0, beatCount = 0;
                    var step = timingPoint.BeatDuration / SnapDivisor;
                    var sectionStartTime = timingPoint.Offset;
                    var sectionEndTime = Math.Min(nextTimingPoint?.Offset ?? rightTime, rightTime);
                    if (timingPoint == leftTimingPoint)
                        while (leftTime < sectionStartTime)
                        {
                            sectionStartTime -= step;
                            tickCount--;
                            if (tickCount % SnapDivisor == 0)
                                beatCount--;
                        }

                    for (var time = sectionStartTime; time < sectionEndTime; time += step)
                    {
                        if (leftTime < time)
                        {
                            var tickColor = tickGrey;
                            var lineSize = new Vector2(1, bounds.Height * 0.4f);

                            var snap = tickCount % SnapDivisor;
                            if (snap == 0) tickColor = tickWhite;
                            else if ((snap * 2) % SnapDivisor == 0) { lineSize.Y *= 0.8f; tickColor = tickRed; }
                            else if ((snap * 3) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickViolet; }
                            else if ((snap * 4) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickBlue; }
                            else if ((snap * 6) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickMagenta; }
                            else if ((snap * 8) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickYellow; }
                            else lineSize.Y *= 0.4f;

                            if (snap != 0 || beatCount % timingPoint.BeatPerMeasure != 0)
                                lineSize.Y *= 0.5f;

                            drawLine(drawContext, offset + new Vector2((int)Math.Floor((time - leftTime) * timeScale), lineBottomY), lineSize, tickColor, actualOpacity);
                        }
                        if (tickCount % SnapDivisor == 0)
                            beatCount++;
                        tickCount++;
                    }
                    timingPoint = nextTimingPoint;
                }
            }

            // Current time
            {
                var centerX = (float)Math.Round(bounds.Width * 0.5);
                var lineSize = new Vector2(1, bounds.Height * 0.2f);
                drawLine(drawContext, offset + new Vector2(centerX - 1, lineBottomY), lineSize, Color4.White, actualOpacity);
                drawLine(drawContext, offset + new Vector2(centerX + 1, lineBottomY), lineSize, Color4.White, actualOpacity);
            }
        }

        private void drawLine(DrawContext drawContext, Vector2 position, Vector2 size, Color4 color, float opacity)
        {
            line.Color = color;
            line.Draw(drawContext, Manager.Camera, new Box2(position, position + size), opacity);
        }

        public void Scroll(float direction)
        {
            var time = Value * 1000.0;
            var timingPoint = project.MainBeatmap.GetTimingPointAt((int)time);

            var stepDuration = timingPoint.BeatDuration / SnapDivisor;
            time += stepDuration * direction;

            var steps = (time - timingPoint.Offset) / stepDuration;
            time = timingPoint.Offset + Math.Round(steps) * stepDuration;

            Value = (float)(time * 0.001);
        }

        public void Snap() => Scroll(0);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                line.Dispose();
            }
            line = null;

            base.Dispose(disposing);
        }
    }
}
