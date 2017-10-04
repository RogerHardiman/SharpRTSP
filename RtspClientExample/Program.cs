using System;
using System.Threading;
using System.Threading.Tasks;
using Rtsp;

namespace RtspClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            //String url = "rtsp://192.168.1.128/ch1.h264";    // IPS
            //String url = "rtsp://192.168.1.125/onvif-media/media.amp?profile=quality_h264"; // Axis
            //String url = "rtsp://user:password@192.168.1.102/onvif-media/media.amp?profile=quality_h264"; // Axis
            //String url = "rtsp://192.168.1.124/rtsp_tunnel?h26x=4&line=1&inst=1"; // Bosch

            //String url = "rtsp://192.168.1.121:8554/h264";  // Raspberry Pi RPOS using Live555
            //String url = "rtsp://192.168.1.121:8554/h264m";  // Raspberry Pi RPOS using Live555 in Multicast mode

            //String url = "rtsp://127.0.0.1:8554/h264ESVideoTest"; // Live555 Cygwin
            //String url = "rtsp://192.168.1.160:8554/h264ESVideoTest"; // Live555 Cygwin
            //String url = "rtsp://127.0.0.1:8554/h264ESVideoTest"; // Live555 Cygwin
             String url = "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov";

            // MJPEG Tests (Payload 26)
            //String url = "rtsp://192.168.1.125/onvif-media/media.amp?profile=mobile_jpeg";


            // Start a Task that continually connects to the RTSP Server until ENTER is pressed
            bool want_video = true;
            bool want_play = false;
            bool want_pause = false;

            // Create a RTSP Client. This is created in a Task so that it can connect (and re-connect)
            // in parallel with the User Interface (readline)
            Task rtspTask = new Task(delegate {
                // Keep looping until want_video is false
                while (want_video) {
                    RTSPClient c = new RTSPClient(url, RTSPClient.RTP_TRANSPORT.TCP);
                    c.Received_SPS32_PPS32 += (byte[] sps32, byte[] pps32) => {
                        Console.WriteLine("Received SPS and PPS from RTSP Client");
                        // Pass this data into a H264 decoder
                    };
                    c.Received_NAL32 += (byte[] nal32) => {
                        Console.WriteLine("Received NALs from RTSP Client");
                        // Pass this data into a H264 decoder
                    };

                    // busy-wait  (//Todo. Change to a better implementation
                    while ((c.CurrentStatus == RtspStatus.Connecting
                            || c.CurrentStatus == RtspStatus.Connected)
                           && want_video == true) {

                        if (want_play) {
                            c.Play();
                            want_play = false;
                        }
                        if (want_pause) {
                            c.Pause();
                            want_pause = false;
                        }
                        Thread.Sleep(250);
                    }

                    // Either CurrentStatus is ConnectionFailed or is Disconnected
                    // OR want_video is now false
                    c.Stop();


                    // Wait 1 second before reconnecting, if we still want video
                    if (want_video == true) Thread.Sleep(1000);
                }
            });
            rtspTask.Start();

            // Wait for user to terminate programme
            // Check for null which is returned when running under some IDEs
            Console.WriteLine("Press 1 then ENTER to play");
            Console.WriteLine("Press 2 then ENTER to pause");
            Console.WriteLine("Press Q then ENTER to quit");

            String readline = null;
            while (want_video == true) {
                readline = Console.ReadLine();

                if (readline == null) {
                    // Avoid maxing out CPU on systems that instantly return null for ReadLine
                    Thread.Sleep(500);
                } else if (readline.Equals("1")) {
                    want_play = true;
                } else if (readline.Equals("2")) {
                    want_pause = true;
                } else if (readline.ToUpper().Equals("Q")) {
                    want_video = false;
                }
            }


            rtspTask.Wait(); // wait for Task to complete before exiting

        }
    }
}
