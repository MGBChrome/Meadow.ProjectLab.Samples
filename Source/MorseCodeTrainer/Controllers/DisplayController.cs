﻿using Meadow.Foundation;
using Meadow.Foundation.Displays.TftSpi;
using Meadow.Foundation.Graphics;
using Meadow.Hardware;
using Meadow.Units;
using System;

namespace MorseCodeTrainer.Controllers
{
    public class DisplayController
    {
        private static readonly Lazy<DisplayController> instance =
            new Lazy<DisplayController>(() => new DisplayController());
        public static DisplayController Instance => instance.Value;

        MicroGraphics graphics;

        private DisplayController() { }

        public void Initialize()
        {
            var config = new SpiClockConfiguration(
                new Frequency(48000, Frequency.UnitType.Kilohertz),
                SpiClockConfiguration.Mode.Mode3);
            var spiBus = MeadowApp.Device.CreateSpiBus(
                clock: MeadowApp.Device.Pins.SCK,
                copi: MeadowApp.Device.Pins.MOSI,
                cipo: MeadowApp.Device.Pins.MISO,
                config: config);
            var display = new St7789
            (
                device: MeadowApp.Device,
                spiBus: spiBus,
                chipSelectPin: MeadowApp.Device.Pins.A03,
                dcPin: MeadowApp.Device.Pins.A04,
                resetPin: MeadowApp.Device.Pins.A05,
                width: 240,
                height: 240,
                displayColorMode: ColorType.Format16bppRgb565
            );
            graphics = new MicroGraphics(display)
            {
                Stroke = 1,
                CurrentFont = new Font12x20(),
                Rotation = RotationType._90Degrees
            };

            graphics.Clear();
            graphics.DrawRectangle(0, 0, graphics.Width, graphics.Height);
            graphics.DrawText(24, 15, "Morse Code Coach");
            graphics.DrawHorizontalLine(24, 41, 196, Color.White);
            graphics.Show();
        }

        public void DrawCorrectIncorrectMessage(string question, string answer, bool isCorrect)
        {
            Color color = isCorrect ? Color.GreenYellow : Color.Red;

            graphics.DrawText(120, 65, question, color, ScaleFactor.X3, TextAlignment.Center);
            UpdateAnswer(answer, color);
            graphics.DrawText(120, 190, isCorrect ? "Correct!" : "Try again!", color, ScaleFactor.X1, TextAlignment.Center);
            graphics.Show();
        }

        public void ShowLetterQuestion(string question)
        {
            graphics.DrawRectangle(2, 65, 236, 60, Color.Black, true);
            graphics.DrawText(120, 65, question, Color.White, ScaleFactor.X3, TextAlignment.Center);
            graphics.DrawRectangle(5, 120, 230, 110, Color.Black, true);
            graphics.Show();
        }

        public void UpdateAnswer(string answer, Color color)
        {
            int x = 0;
            int y = 143;

            switch (answer.Length)
            {
                case 1: x = 109; break;
                case 2: x = 96; break;
                case 3: x = 83; break;
                case 4: x = 71; break;
                case 5: x = 57; break;
                default: return;
            }

            graphics.DrawRectangle(24, y, 200, 30, Color.Black, true);

            foreach (var ch in answer)
            {
                DrawDashOrDot(x, y, ch == '-', color);
                x += 26;
            }

            graphics.Show();
        }

        void DrawDashOrDot(int x, int y, bool isDash, Color color)
        {
            if (isDash)
            {
                graphics.DrawRectangle(x + 3, y + 6, 20, 8, color, true);
            }
            else
            {
                graphics.DrawCircle(x + 11, y + 10, 10, color, true);
            }

            graphics.Show();
        }
    }
}