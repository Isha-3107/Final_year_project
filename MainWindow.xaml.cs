using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace KinectSkeletonWPF
{

    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private string gestureLabel;
        private string outputPath;
        private List<object> sessionFrames = new List<object>();
        private bool isRecording = false; // Flag to control recording
        private readonly double scale = 180;     // Scale down to half size (150 instead of 300)
        private readonly double offsetX = 400;   // Horizontal offset (adjust as needed)
        private readonly double offsetY = 300;   // Vertical offset (adjust as needed)

        // ✅ Moved kinectEdges to class-level
        private readonly (JointType, JointType)[] kinectEdges =
        {
            (JointType.Head, JointType.ShoulderCenter),
            (JointType.ShoulderCenter, JointType.ShoulderLeft),
            (JointType.ShoulderCenter, JointType.ShoulderRight),
            (JointType.ShoulderCenter, JointType.Spine),
            (JointType.ShoulderLeft, JointType.ElbowLeft),
            (JointType.ElbowLeft, JointType.WristLeft),
            (JointType.WristLeft, JointType.HandLeft),
            (JointType.ShoulderRight, JointType.ElbowRight),
            (JointType.ElbowRight, JointType.WristRight),
            (JointType.WristRight, JointType.HandRight),
            (JointType.Spine, JointType.HipCenter),
            (JointType.HipCenter, JointType.HipLeft),
            (JointType.HipCenter, JointType.HipRight),
            (JointType.HipLeft, JointType.KneeLeft),
            (JointType.KneeLeft, JointType.AnkleLeft),
            (JointType.AnkleLeft, JointType.FootLeft),
            (JointType.HipRight, JointType.KneeRight),
            (JointType.KneeRight, JointType.AnkleRight),
            (JointType.AnkleRight, JointType.FootRight)
        };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += Window_Closed;

            // ✅ Set fixed size to match HTML .skeletal_window
            this.Width = 750;
            this.Height = 500;

            // ✅ Calculate position to center inside 900px card on 1920px screen
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double leftCardWidth = 900;
            double skeletalWidth = 700;
            double offsetLeft = (leftCardWidth - skeletalWidth) / 2;
            this.Left = (screenWidth - leftCardWidth) / 2 + offsetLeft;

            // ✅ Adjust vertical position to match heading and spacing in HTML
            this.Top = 200;

            // ✅ Optional: Remove border and resizing to appear embedded
            // this.WindowStyle = WindowStyle.None;
            // this.ResizeMode = ResizeMode.NoResize;
            // this.Topmost = true; // Optional: keep always on top
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (KinectSensor.KinectSensors.Count > 0)
                {
                    sensor = KinectSensor.KinectSensors[0];

                    if (sensor != null)
                    {
                        sensor.SkeletonStream.Enable();
                        sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;
                        sensor.Start();
                        Console.WriteLine("✅ Kinect Started. Ready for Gesture Recording.");
                    }
                    else
                    {
                        MessageBox.Show("Kinect detected but could not start.");
                    }
                }
                else
                {
                    MessageBox.Show("⚠ No Kinect sensor detected. Please connect a Kinect sensor.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Kinect: {ex.Message}");
            }
        }

        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!isRecording) return; // Only process frames when recording is ON

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    DrawSkeleton(skeletons);
                    CollectSkeletonData(skeletons);
                }
            }
        }

        private void DrawSkeleton(Skeleton[] skeletons)
        {
            SkeletonCanvas.Children.Clear(); // Clear previous frame

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    foreach (Joint joint in skeleton.Joints)
                    {
                        if (joint.TrackingState == JointTrackingState.Tracked)
                        {
                            DrawJoint(joint);
                        }
                    }

                    // Draw edges (bones) between joints
                    DrawSkeletonEdges(skeleton);
                }
            }
        }

        private void DrawSkeletonEdges(Skeleton skeleton)
        {
            var jointPoints = new Dictionary<JointType, Point>();

            // Convert joint positions to screen coordinates
            foreach (Joint joint in skeleton.Joints)
            {
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    double x = joint.Position.X * scale + offsetX;
                    double y = -joint.Position.Y * scale + offsetY;
                    jointPoints[joint.JointType] = new Point(x, y);
                }
            }

            // Draw lines (bones) between connected joints
            foreach (var edge in kinectEdges)
            {
                if (jointPoints.ContainsKey(edge.Item1) && jointPoints.ContainsKey(edge.Item2))
                {
                    DrawBone(jointPoints[edge.Item1], jointPoints[edge.Item2]);
                }
            }
        }

        private void DrawBone(Point start, Point end)
        {
            Line line = new Line
            {
                Stroke = Brushes.White,
                StrokeThickness = 3,
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y
            };

            SkeletonCanvas.Children.Add(line);
        }

        private void DrawJoint(Joint joint)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red
            };

            Canvas.SetLeft(ellipse, joint.Position.X * scale + offsetX);
            Canvas.SetTop(ellipse, -joint.Position.Y * scale + offsetY);
            SkeletonCanvas.Children.Add(ellipse);
        }

        private void CollectSkeletonData(Skeleton[] skeletons)
        {
            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    var joints = new Dictionary<string, object>();
                    var edges = new List<object>();

                    foreach (Joint joint in skeleton.Joints)
                    {
                        joints[joint.JointType.ToString()] = new
                        {
                            x = joint.Position.X,
                            y = joint.Position.Y,
                            z = joint.Position.Z
                        };
                    }

                    // ✅ Using class-level kinectEdges
                    foreach (var edge in kinectEdges)
                    {
                        edges.Add(new { start = edge.Item1.ToString(), end = edge.Item2.ToString() });
                    }

                    sessionFrames.Add(new { joints = joints, edges = edges });
                }
            }
        }

        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            outputPath = $"D:\\KinectData\\gesture_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";
            Directory.CreateDirectory("D:\\KinectData");

            sessionFrames.Clear(); // Clear previous session data
            isRecording = true;
            Console.WriteLine($"🎥 Recording started...");
        }
        // Function to send JSON data to Flask



        private async Task SendDataToServer(string jsonData)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    Console.WriteLine("📤 Sending JSON to Flask Server...");  // Debug print
                    Console.WriteLine(jsonData);  // Print JSON in console

                    HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:5000/receive_json", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ JSON Data successfully sent to the server.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Error sending data: {response.StatusCode}");
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Server Response: {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
            }
        }

        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            isRecording = false;

            if (sessionFrames.Count > 0)
            {
                var sessionData = new { frames = sessionFrames }; // Removed label
                File.WriteAllText(outputPath, JsonSerializer.Serialize(sessionData, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"✅ Gesture data saved to: {outputPath}");

                SendDataToServer(JsonSerializer.Serialize(sessionData, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("⚠ No skeleton data captured. JSON file not created.");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (sensor != null && sensor.IsRunning)
            {
                sensor.Stop();
                Console.WriteLine("🛑 Kinect Stopped.");
            }
        }
    }
}