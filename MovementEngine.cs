using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KSWorks
{
    public class MovementEngine
    {
        private CancellationTokenSource _cts;
        private Task _workerTask;
        private readonly Random _random = new Random();

        // Configuration
        public bool IsRunning { get; private set; }
        public int ActivityIntervalMs { get; set; } = 3000; // Move roughly every 3 seconds
        public int MovementStrength { get; set; } = 100; // Offset distance (Strength)
        public int MovementDurationMs { get; set; } = 2000; // Duration of movement action

        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            IsRunning = true;

            // Start the background worker task to avoid blocking the UI thread
            _workerTask = Task.Run(async () => await SimulationLoop(_cts.Token), _cts.Token);
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _cts.Cancel();
            try
            {
                _workerTask.Wait();
            }
            catch (AggregateException) { /* Task cancelled */ }
            finally
            {
                _cts.Dispose();
                _workerTask = null;
                IsRunning = false;
            }
        }

        private async Task SimulationLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for the next movement interval, adding PRNG jitter to avoid telemetry detection
                int jitter = _random.Next(-500, 500); 
                int delay = Math.Max(100, ActivityIntervalMs + jitter);
                
                try
                {
                    await Task.Delay(delay, token);
                }
                catch (TaskCanceledException)
                {
                    break; // Stop requested during delay
                }

                if (token.IsCancellationRequested) break;

                ExecuteRealisticMovement();
            }
        }

        private void ExecuteRealisticMovement()
        {
            // Pick a random relative target based on customizable Strength
            int targetX = _random.Next(-MovementStrength, MovementStrength);
            int targetY = _random.Next(-MovementStrength, MovementStrength);
            int steps = _random.Next(20, 50); // Number of interpolation steps

            var path = PathGenerator.GenerateBezierPath(new Point(0, 0), new Point(targetX, targetY), steps);

            Point currentPos = new Point(0, 0);

            // Calculate precise sleep time based on user-defined movement duration
            int baseSleep = Math.Max(2, MovementDurationMs / steps);

            foreach (var point in path)
            {
                // Calculate the delta to the next point on the path
                int dx = (int)Math.Round(point.X - currentPos.X);
                int dy = (int)Math.Round(point.Y - currentPos.Y);

                if (dx != 0 || dy != 0)
                {
                    SendRelativeMouseMovement(dx, dy);
                    currentPos.X += dx;
                    currentPos.Y += dy;
                }

                // Smooth micro-sleep between steps
                Thread.Sleep(baseSleep + _random.Next(-1, 2)); 
            }
        }

        private void SendRelativeMouseMovement(int dx, int dy)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].mi = new NativeMethods.MOUSEINPUT
            {
                dx = dx,
                dy = dy,
                mouseData = 0,
                dwFlags = NativeMethods.MOUSEEVENTF_MOVE,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}
