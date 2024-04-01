
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Statistics;
using NAudio.Dsp;
using NAudio.Gui;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.IO.Ports;
using System.Net.WebSockets;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

//using static System.Windows.Forms.AxHost;
using Complex = System.Numerics.Complex;

namespace V10__major_
{
    public partial class Form1 : Form
    {

        string line;

        // Declare the WebSocket client
        private WebSocket espWebSocketClient;

        private const int bufferSize = 1024;
        private WasapiLoopbackCapture audioCapture;
        private float[] audioBuffer = new float[bufferSize];
        private Complex[] fftBuffer = new Complex[bufferSize];

        // Define frequency band ranges
        private int subBassMin = 20;
        private int subBassMax = 60;
        private int bassMin = 60;
        private int bassMax = 250;
        private int lowerMidrangeMin = 250;
        private int lowerMidrangeMax = 500;
        private int midrangeMin = 500;
        private int midrangeMax = 2000;
        private int higherMidrangeMin = 2000;
        private int higherMidrangeMax = 4000;
        private int presenceMin = 4000;
        private int presenceMax = 6000;
        private int brillianceMin = 6000;
        private int brillianceMax = 20000;

        string PrevVal;

        private bool isDragging = false;
        private Point clickPoint;

        bool startFlag;
        int initialVal = 0;

        private int windowSize = 10; // Adjust the window size as needed
        private double[] pastValues = new double[10]; // Store the past N values
        private int pastValueIndex = 0;


        private double runningAverageEnergy = 0.0;
        //private int numFramesForRunningAverage = 100; // Adjust as needed
        private int numFramesForRunningAverage = 70; // Adjust as needed
                                                     //  private double thresholdMultiplier = 1.2; // Adjust as needed
       // private double thresholdMultiplier = 1.5; // Adjust as needed
        private double thresholdMultiplier = 1.5; // Adjust as needed


        // Start the thread for sending data to ESP module
        Thread sendToEspThread;


        double originalValue; // Replace with your original value
        double minValue = 0;     // Minimum value in the original range
        double maxValue = 1800;    // Maximum value in the original range

        // Flag to prevent rapid detection of the same hit
        private bool snareDetected = false;
        private bool highHatDetected = false;




        // private double hiHatEnergyThreshold = 50.0; // Adjust this threshold as needed
        private double hiHatEnergyThreshold = 70; // Adjust this threshold as needed

        private double previousHiHatEnergy = 0.0;

        private DateTime lastHiHatHitTime = DateTime.MinValue; // Initialize with a very old timestamp
        private int debouncePeriod = 120; // Debounce period in milliseconds


        
        private double emaAlpha = 0.2; // Adjust the alpha value as needed (0 < alpha < 1)
        private double emaSubBass = 0.0; // Initialize the EMA for sub-bass


        private double[] lowPassBuffer = new double[bufferSize];
        //private double lowPassAlpha = 0.1; // Adjust the alpha value as needed (0 < alpha < 1)
        private double lowPassAlpha = 0.5;
        public Form1()
        {
            InitializeComponent();

            // Initialize audio capture from system audio
            audioCapture = new WasapiLoopbackCapture();

            audioCapture.DataAvailable += OnDataAvailable;
            audioCapture.StartRecording();

            //sendToEspThread = new Thread(SendDataToEspThread);
            b_Subbass.IsHorizontal = false;
            b_Bass.IsHorizontal = false;
            b_Mid.IsHorizontal = false;
            b_LM.IsHorizontal = false;
            b_HM.IsHorizontal = false;
            b_presence.IsHorizontal = false;
            b_briliance.IsHorizontal = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Sub_Bar.ProgressValue = 70;

            //Sub_Bar.IsHorizontal = false;

            startFlag = true;

            //sendToEspThread.Start();


            //Task.Run(async () =>
            //{
            //    // Simulate an asynchronous operation
            //    await EstablishESPWebSocketConnection();
            //});



        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {

            float[] audioData = new float[e.BytesRecorded / 4]; // Assuming 32-bit float samples

            // Calculate the number of samples based on the buffer size and sample size
            int samplesRead = e.BytesRecorded / (audioCapture.WaveFormat.BitsPerSample / 8);
            int subDif, bassDif, lowerMidDif, midDif, higherMidDif, presensceDif, brillianceDif;

            int sub, bass, Higher, Mid, Lower, Presence, Brilliance;

            int kick, snare;
            int[] freqDiff = new int[7];

            // Calculate spectral flux (as previously explained)
            double[] spectralFlux = new double[samplesRead];


            // Ensure that the audio buffer is the correct size
            if (samplesRead != audioBuffer.Length)
            {
                Array.Resize(ref audioBuffer, samplesRead);
                fftBuffer = new Complex[samplesRead]; // Resize fftBuffer accordingly


            }



            // Convert the byte buffer to a float array
            Buffer.BlockCopy(e.Buffer, 0, audioBuffer, 0, e.BytesRecorded);

            // Ensure that the audio buffer and low-pass buffer have the same size
            if (audioBuffer.Length != lowPassBuffer.Length)
            {
                // Resize the low-pass buffer to match the audio buffer size
                lowPassBuffer = new double[audioBuffer.Length];
            }

            //// Apply the low-pass filter to the audio data
            //for (int i = 0; i < audioBuffer.Length; i++)
            //{
            //    lowPassBuffer[i] = (lowPassAlpha * audioBuffer[i]) + ((1 - lowPassAlpha) * lowPassBuffer[i]);
            //}

            // Step 2: Perform FFT analysis
            for (int i = 0; i < samplesRead; i++)
            {
                fftBuffer[i] = new Complex(audioBuffer[i], 0);
            }


            Fourier.Forward(fftBuffer, FourierOptions.NoScaling);


            //volumeMeter1.Amplitude = CalculateAmplitude(e.Buffer, e.BytesRecorded);
            // Step 3: Calculate energy in frequency bands
            double subBassEnergy = CalculateBandEnergy(subBassMin, subBassMax);
            double bassEnergy = CalculateBandEnergy(bassMin, bassMax);
            double LowerMidEnergy = CalculateBandEnergy(lowerMidrangeMin, lowerMidrangeMax);
            double MidEnergy = CalculateBandEnergy(midrangeMin, midrangeMax);
            double HigherMidEnergy = CalculateBandEnergy(higherMidrangeMin, higherMidrangeMax);
            double PresenceEnergy = CalculateBandEnergy(presenceMin, presenceMax);
            double BrillianceEnergy = CalculateBandEnergy(brillianceMin, brillianceMax);

            //do not delete this code. this part could be turned into a feature
            // Apply EMA smoothing to sub-bass energy
            //emaSubBass = (emaAlpha * bassEnergy) + ((1 - emaAlpha) * emaSubBass);
            //bassEnergy = emaSubBass;
            //^

            // Calculate energy for other frequency bands...

            // Step 4: Update ProgressBar controls with energy levels on the UI thread
            BeginInvoke(new Action(() =>
            {
                
                runningAverageEnergy = CalculateRunningAverageEnergy(audioBuffer, numFramesForRunningAverage);

                // Calculate the adaptive threshold
                double adaptiveThreshold = thresholdMultiplier * runningAverageEnergy;

                double scaledValue = ((adaptiveThreshold - minValue) / (maxValue - minValue)) * 100;
                label4.Text = scaledValue.ToString();

                if (scaledValue >= 120)
                {
                    BK_Kick.BackColor = Color.Red;
                }
                else
                {
                    BK_Kick.BackColor = Color.White;
                }
                b_Subbass.ProgressValue = MapEnergyToTrackBarValue(subBassEnergy);
                b_Bass.ProgressValue = MapEnergyToTrackBarValue(bassEnergy);
                b_LM.ProgressValue = MapEnergyToTrackBarValue(LowerMidEnergy);
                b_Mid.ProgressValue = MapEnergyToTrackBarValue(MidEnergy);
                b_HM.ProgressValue = MapEnergyToTrackBarValue(HigherMidEnergy);
                b_presence.ProgressValue = MapEnergyToTrackBarValue(PresenceEnergy);
                b_briliance.ProgressValue = MapEnergyToTrackBarValue(BrillianceEnergy);

                Val_SubBass.Text = b_Subbass.ProgressValue.ToString();
                Val_Bass.Text = b_Bass.ProgressValue.ToString();
                Val_LM.Text = b_LM.ProgressValue.ToString();
                Val_Mid.Text = b_Mid.ProgressValue.ToString();
                Val_HM.Text = b_HM.ProgressValue.ToString();
                Val_Presence.Text = b_presence.ProgressValue.ToString();
                Val_Brilliance.Text = b_briliance.ProgressValue.ToString();

                kick = Convert.ToInt32(Val_Bass.Text);
                snare = Convert.ToInt32(Val_HM.Text);


                pastValues[pastValueIndex] = Convert.ToInt32(Val_SubBass.Text);

                // Increment the index for storing the next past value
                pastValueIndex = (pastValueIndex + 1) % windowSize;

                // Calculate the moving average for the sub-bass band
                double subBassAverage = CalculateMovingAverage(pastValues);

                // Calculate the difference using the moving average
                int subBassDiff = (int)(kick - subBassAverage);

                label5.Text = subBassDiff.ToString();

                //if (sub == 100)
                //{
                // Send the line to the ESP module
                //label13.Text = RGBStringToBinary(line);
                //label13.Text = line;
                //SendLineToEspWebSocket(line);
                //}

                //label14.Text = line;



                if (BrillianceEnergy > previousHiHatEnergy + hiHatEnergyThreshold)
                {
                    // Hi-hat hit detected
                    // Add your logic here, e.g., change color or play a sound
                    // You can also consider debouncing to prevent multiple detections for a single hit

                    // Calculate the time elapsed since the last hi-hat hit
                    TimeSpan timeSinceLastHit = DateTime.Now - lastHiHatHitTime;

                    // Check if it's a new hi-hat hit (debounced)
                    if (timeSinceLastHit.TotalMilliseconds >= debouncePeriod)
                    {
                        // Hi-hat hit detected

                        Bk_HiHat.BackColor = Color.Orange;
                        // Update the timestamp for the last hi-hat hit
                        lastHiHatHitTime = DateTime.Now;
                    }

                }
                else
                {
                    Bk_HiHat.BackColor = Color.White;
                }

                // Store the current energy level as the previous energy level for the next iteration
                previousHiHatEnergy = HigherMidEnergy;

                if (snare >= 80)
                {
                    Bk_Snare.BackColor = Color.Green;
                }
                else
                {
                    Bk_Snare.BackColor = Color.White;
                }

            }));

            if (startFlag == true)
            {
                startFlag = false;
                PrevVal = line;
            }

        }

        private double CalculateBandEnergy(int minFrequency, int maxFrequency)
        {
            int minIndex = (int)Math.Round((minFrequency / (double)audioCapture.WaveFormat.SampleRate) * audioBuffer.Length);
            int maxIndex = (int)Math.Round((maxFrequency / (double)audioCapture.WaveFormat.SampleRate) * audioBuffer.Length);

            double energy = 0.0;
            for (int i = minIndex; i <= maxIndex && i < fftBuffer.Length; i++)
            {
                energy += fftBuffer[i].Magnitude;
            }

            return energy;
        }

        private int MapEnergyToTrackBarValue(double energy)
        {
            // Define the minimum and maximum values of energy and the track bar's minimum and maximum values
            double energyMin = 0;            // Minimum energy value
                                             // double energyMax = 2800;         // Maximum energy value (adjust as needed)
            double energyMax = 2800;
            int trackBarMinValue = 0;        // Minimum value of the track bar
            int trackBarMaxValue = 100;      // Maximum value of the track bar

            // Calculate the mapped value using linear mapping
            int mappedValue = (int)(((energy - energyMin) / (energyMax - energyMin)) * (trackBarMaxValue - trackBarMinValue)) + trackBarMinValue;

            // Ensure the mapped value is within the track bar's range
            if (mappedValue < trackBarMinValue)
            {
                mappedValue = trackBarMinValue;
            }
            else if (mappedValue > trackBarMaxValue)
            {
                mappedValue = trackBarMaxValue;
            }

            return mappedValue;
        }

       

        private double CalculateMovingAverage(double[] values)
        {
            // Calculate the moving average of an array of values
            double sum = 0;
            foreach (double value in values)
            {
                sum += value;
            }
            return sum / values.Length;
        }

        private double CalculateRunningAverageEnergy(float[] audioBuffer, int numFrames)
        {
            double sumEnergy = 0.0;
            int numFramesToAverage = Math.Min(numFrames, audioBuffer.Length / bufferSize);

            for (int i = 0; i < numFramesToAverage; i++)
            {
                // Calculate energy for the current frame (similar to your existing code)
                // For example, using CalculateBandEnergy method for sub-bass band
                double subBassEnergy = CalculateBandEnergy(subBassMin, subBassMax);

                // Accumulate the energy
                sumEnergy += subBassEnergy;

                // Move to the next frame (you may need to adjust buffer indexing)
                // ...
            }

            // Calculate the running average
            return sumEnergy / numFramesToAverage;
        }

        void CalculateRunningStatistics(double[] data, double[] statistics)
        {
            double sum = 0;
            double sumSquared = 0;

            foreach (double value in data)
            {
                sum += value;
                sumSquared += value * value;
            }

            int n = data.Length;
            double mean = sum / n;
            double variance = (sumSquared - (sum * sum) / n) / (n - 1);
            double stdDeviation = Math.Sqrt(variance);

            statistics[0] = mean;
            statistics[1] = stdDeviation;
        }




    }
}