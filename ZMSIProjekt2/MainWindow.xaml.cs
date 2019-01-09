using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.IO;

using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

// Kod został napisany samodzielnie
// Członkowie grupy: Piotr Bachur, Krzysztof Marciniak

namespace ZMSIProjekt2
{
    public class SingleIteration
    {
        public int[] inputs;
        public double[] weights;
        public int expectedResult;
        public double sumResult;
        public int uniPolarResult;
        public bool isOK;
    }

    public partial class MainWindow : Window
    {
        double learningCoefficient;
        int amountOfLearningPoints = -1;
        double[] weights = new double[3];
        double separatingFunctionA;
        double separatingFunctionB;
        PlotModel plotModel = new PlotModel();
        PlotModel learningSetPlotModel = new PlotModel();
        int[,] learningSet = null;
        int currentIteration = 0;
        Brush originalButtonBackgroundColor;
        bool valuesOK = false;
        bool autoUpdate = false;

        ScatterSeries scatterSeriesAbove;
        ScatterSeries scatterSeriesBelow;
        FunctionSeries lineFunctionSeries = null;

        LinearAxis xAxis;
        LinearAxis yAxis;

        LinearAxis xAxisLearningSet;
        LinearAxis yAxisLearningSet;

        List<SingleIteration> iterationHistory;
        List<SingleIteration> currentIterationsInEpoch;

        public MainWindow ()
        {
            currentIterationsInEpoch = new List<SingleIteration>();
            iterationHistory = new List<SingleIteration>();
            scatterSeriesAbove = new ScatterSeries { MarkerType = MarkerType.Circle, Title = "Punkty nad granicą decyzyjną (według perceptronu)" };
            scatterSeriesBelow = new ScatterSeries { MarkerType = MarkerType.Circle, Title = "Punkty pod granicą decyzyjną (według perceptronu)" };

            InitializeComponent();

            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
            CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");

            xAxis = new LinearAxis { IsZoomEnabled = false, IsPanEnabled = false, Position = AxisPosition.Bottom, Minimum = -13, Maximum = 13, PositionAtZeroCrossing = true, ExtraGridlines = new double[] { 0 } };
            plotModel.Axes.Add(xAxis);
            yAxis = new LinearAxis { IsZoomEnabled = false, IsPanEnabled = false, Position = AxisPosition.Left, Minimum = -13, Maximum = 13, PositionAtZeroCrossing = true, ExtraGridlines = new double[] { 0 } };
            plotModel.Axes.Add(yAxis);
            plotModel.PlotMargins = new OxyPlot.OxyThickness(0);

            plotModel.Series.Add(scatterSeriesAbove);
            plotModel.Series.Add(scatterSeriesBelow);

            plot.LayoutUpdated += plotLayoutUpdated;
            plot.Model = plotModel;

            xAxisLearningSet = new LinearAxis { IsZoomEnabled = false, IsPanEnabled = false, Position = AxisPosition.Bottom, Minimum = -13, Maximum = 13, PositionAtZeroCrossing = true, ExtraGridlines = new double[] { 0 } };
            learningSetPlotModel.Axes.Add(xAxisLearningSet);
            yAxisLearningSet = new LinearAxis { IsZoomEnabled = false, IsPanEnabled = false, Position = AxisPosition.Left, Minimum = -13, Maximum = 13, PositionAtZeroCrossing = true, ExtraGridlines = new double[] { 0 } };
            learningSetPlotModel.Axes.Add(yAxisLearningSet);
            learningSetPlotModel.PlotMargins = new OxyPlot.OxyThickness(0);

            learningSetPlot.LayoutUpdated += learningSetPlotLayoutUpdated;
            learningSetPlot.Model = learningSetPlotModel;
            originalButtonBackgroundColor = generateLearningSetButton.Background;
        }

        private void plotLayoutUpdated (object sender, EventArgs e)
        {
            GroupBox parent = (GroupBox)plot.Parent;

            if (parent.ActualWidth < parent.ActualHeight) {
                plot.Height = parent.ActualWidth - 10;
                plot.Width = parent.ActualWidth - 10;
            } else {
                plot.Height = parent.ActualHeight - 10;
                plot.Width = parent.ActualHeight - 10;
            }
        }

        private void learningSetPlotLayoutUpdated (object sender, EventArgs e)
        {
            GroupBox parent = (GroupBox)learningSetPlot.Parent;

            if (parent.ActualWidth < parent.ActualHeight) {
                learningSetPlot.Height = parent.ActualWidth - 10;
                learningSetPlot.Width = parent.ActualWidth - 10;
            } else {
                learningSetPlot.Height = parent.ActualHeight - 10;
                learningSetPlot.Width = parent.ActualHeight - 10;
            }
        }

        private bool checkAmountLearningSetPoints ()
        {
            bool result = true;

            amountOfLearningPointsTextBox.Background = Brushes.White;

            if (!int.TryParse(amountOfLearningPointsTextBox.Text, out amountOfLearningPoints) || amountOfLearningPoints < 1) {
                amountOfLearningPointsTextBox.Background = Brushes.Red;
                result = false;
            }

            return result;
        }

        private bool checkSeparationFunctionValues ()
        {
            bool separtatingFunctionParametersOK = true;
            IEnumerable<TextBox> separatingFunctionTextBoxes = separatingFunctionParametersGrid.Children.OfType<TextBox>();
            int i = 0;
            foreach (TextBox textBox in separatingFunctionTextBoxes) {
                textBox.Background = Brushes.White;
                double currentParameter = 0;

                if (!double.TryParse(textBox.Text, out currentParameter)) {
                    textBox.Background = Brushes.Red;
                    separtatingFunctionParametersOK = false;
                } else {
                    if (i == 0) {
                        separatingFunctionA = currentParameter;
                    } else {
                        separatingFunctionB = currentParameter;
                    }
                }

                i++;
            }

            if (separtatingFunctionParametersOK && lineFunctionSeries == null) {
                Func<double, double> lineFunction = input => separatingFunctionA * input + separatingFunctionB;
                lineFunctionSeries = new FunctionSeries(lineFunction, -15, 15, 15.0);
                plotModel.Series.Add(lineFunctionSeries);
                plotModel.InvalidatePlot(true);
            }

            return separtatingFunctionParametersOK;
        }

        private void checkVariables()
        {
            valuesOK = true;

            learningCoefficientTextBox.Background = Brushes.White;

            if (!double.TryParse(learningCoefficientTextBox.Text, out learningCoefficient)) {
                learningCoefficientTextBox.Background = Brushes.Red;
                valuesOK = false;
            }

            if (!checkAmountLearningSetPoints()) {
                valuesOK = false;
            }

            IEnumerable<TextBox> weightsTextBoxes = weightsGrid.Children.OfType<TextBox>();
            int i = 0;
            foreach (TextBox textBox in weightsTextBoxes) {
                textBox.Background = Brushes.White;
                double currentWeight = 0;

                if (!double.TryParse(textBox.Text, out currentWeight)) {
                    textBox.Background = Brushes.Red;
                    valuesOK = false;
                } else {
                    weights[i] = currentWeight;
                }

                i++;
            }

            if (!checkSeparationFunctionValues()) {
                valuesOK = false;
            }

            generateLearningSetButton.Background = originalButtonBackgroundColor;
            if (learningSet == null) {
                generateLearningSetButton.Background = Brushes.Red;
                valuesOK = false;
            }
        }

        private void setIterationsExpectedResult(ref SingleIteration singleIteration)
        {
            singleIteration.expectedResult = (singleIteration.inputs[2] > (separatingFunctionA * singleIteration.inputs[1] + separatingFunctionB)) ? 1 : 0;
        }

        private void runSingleLearningStep()
        {
            if (!valuesOK) {
                checkVariables();
            }

            if (valuesOK && !checkIfCurrentEpochIsCorrect()) {
                if (currentIteration >= amountOfLearningPoints) {
                    currentIteration = 0;
                    currentIterationsInEpoch.Clear();
                    scatterSeriesAbove.Points.Clear();
                    scatterSeriesBelow.Points.Clear();

                    plotModel.InvalidatePlot(true);
                }

                SingleIteration iterationResult = new SingleIteration();
                iterationResult.inputs = new int[3];
                iterationResult.inputs[0] = 1;
                iterationResult.inputs[1] = learningSet[currentIteration, 0];
                iterationResult.inputs[2] = learningSet[currentIteration, 1];

                iterationResult.weights = new double[3];
                iterationResult.weights[0] = weights[0];
                iterationResult.weights[1] = weights[1];
                iterationResult.weights[2] = weights[2];

                setIterationsExpectedResult(ref iterationResult);

                for (int i = 0; i < 3; i++) {
                    iterationResult.sumResult += iterationResult.inputs[i] * iterationResult.weights[i];
                }

                iterationResult.sumResult = Math.Round(iterationResult.sumResult, 3);

                iterationResult.uniPolarResult = iterationResult.sumResult > 0 ? 1 : 0;
                iterationResult.isOK = iterationResult.uniPolarResult == iterationResult.expectedResult;

                if (!iterationResult.isOK) {
                    updateWeights(iterationResult);
                }

                currentIterationsInEpoch.Add(iterationResult);
                iterationHistory.Add(iterationResult);

                currentIteration++;

                displayCurrentEpochCheckedPoints();
            }
        }

        private void updateWeights(SingleIteration iterationValues)
        {
            autoUpdate = true;

            IEnumerable<TextBox> weightsTextBoxes = weightsGrid.Children.OfType<TextBox>();
            for (int i = 0; i < 3; i++) {
                weights[i] = weights[i] + (learningCoefficient * (iterationValues.expectedResult - iterationValues.uniPolarResult) * iterationValues.inputs[i]);
                weightsTextBoxes.ElementAt(i).Text = weights[i].ToString();
            }

            autoUpdate = false;
        }

        private void displayCurrentEpochCheckedPoints()
        {
            foreach (SingleIteration iterationResult in currentIterationsInEpoch) {
                if (iterationResult.uniPolarResult == 1) {
                    scatterSeriesAbove.Points.Add(new ScatterPoint(iterationResult.inputs[1], iterationResult.inputs[2], 5));
                } else { 
                    scatterSeriesBelow.Points.Add(new ScatterPoint(iterationResult.inputs[1], iterationResult.inputs[2], 5));
                }
            }

            amountOfStepsTakenLabel.Content = iterationHistory.Count;

            plotModel.InvalidatePlot(true);
        }

        private void runSingleLearningStepClick (object sender, RoutedEventArgs e)
        {
            runSingleLearningStep();
        }

        private void generateLearningSet (object sender, RoutedEventArgs e)
        {
            invalidateCurrentRunValues();

            if (checkAmountLearningSetPoints()) {
                Random random = new Random();
                learningSet = new int[amountOfLearningPoints, 2];

                learningSetPlotModel.Series.Clear();
                ScatterSeries scatterSeries = new ScatterSeries { Title = "Zbiór uczący", MarkerType = MarkerType.Circle };

                for (int i = 0; i < amountOfLearningPoints; i++) {
                    int x = random.Next(-10, 11);
                    int y = random.Next(-10, 11);

                    learningSet[i, 0] = x;
                    learningSet[i, 1] = y;

                    scatterSeries.Points.Add(new ScatterPoint(x, y, 5));
                }

                learningSetPlotModel.Series.Add(scatterSeries);
                learningSetPlotModel.InvalidatePlot(true);
                generateLearningSetButton.Background = originalButtonBackgroundColor;
            }
        }

        private void getRandomWeights (object sender, RoutedEventArgs e)
        {
            invalidateCurrentRunValues();

            bool rangeOK = true;
            double rangeMin = 0;
            double rangeMax = 0;
            IEnumerable<TextBox> rangeTextBoxes = weightsRangeGrid.Children.OfType<TextBox>();
            int i = 0;
            foreach (TextBox textBox in rangeTextBoxes) {
                double currentRangeValue = 0;
                textBox.Background = Brushes.White;

                if (double.TryParse(textBox.Text, out currentRangeValue)) {
                    if (i == 0) {
                        rangeMin = currentRangeValue;
                    } else {
                        if (rangeMin < currentRangeValue) {
                            rangeMax = currentRangeValue;
                        } else {
                            textBox.Background = Brushes.Red;
                            rangeOK = false;
                        }
                    }
                } else {
                    textBox.Background = Brushes.Red;
                    rangeOK = false;
                }

                i++;
            }

            if (rangeOK) {
                IEnumerable<TextBox> weightsTextBoxes = weightsGrid.Children.OfType<TextBox>();
                int j = 0;
                Random random = new Random();
                foreach (TextBox textBox in weightsTextBoxes) {
                    textBox.Background = Brushes.White;
                    double rangeSpan = Math.Abs(rangeMin - rangeMax);

                    weights[j] = Math.Round(((random.NextDouble() * rangeSpan) + rangeMin), 3);
                    textBox.Text = weights[j].ToString();

                    j++;
                }
            }
        }

        private void invalidateSeparationFunction (object sender, TextChangedEventArgs e)
        {
            invalidateCurrentRunValues();

            if (lineFunctionSeries != null) {
                plotModel.Series.Remove(lineFunctionSeries);
            }

            lineFunctionSeries = null;

            IEnumerable<TextBox> separatingFunctionTextBoxes = separatingFunctionParametersGrid.Children.OfType<TextBox>();
            int boxesOk = 0;
            double value;
            foreach (TextBox textBox in separatingFunctionTextBoxes) {
                if (double.TryParse(textBox.Text, out value)) {
                    boxesOk++;
                }
            }

            if (boxesOk == 2) {
                checkSeparationFunctionValues();
            }
        }

        private bool checkIfCurrentEpochIsCorrect()
        {
            bool epochIsCorrect = false;

            if (currentIterationsInEpoch.Count == amountOfLearningPoints) {
                int correctCount = 0;
                foreach (SingleIteration singleIteration in currentIterationsInEpoch) {
                    if (singleIteration.isOK) {
                        correctCount++;
                    } else {
                        break;
                    }
                }

                if (correctCount == amountOfLearningPoints) {
                    epochIsCorrect = true;
                }
            }

            return epochIsCorrect;
        }

        private void startAutoLearn (object sender, RoutedEventArgs e)
        {

            if (!valuesOK) {
                checkVariables();
            }

            while (!checkIfCurrentEpochIsCorrect() && valuesOK) {
                runSingleLearningStep();
            }
        }

        private void invalidateCurrentRunValues()
        {
            iterationHistory.Clear();
            scatterSeriesAbove.Points.Clear();
            scatterSeriesBelow.Points.Clear();
            currentIterationsInEpoch.Clear();
            currentIteration = 0;

            if (amountOfStepsTakenLabel != null) {
                amountOfStepsTakenLabel.Content = iterationHistory.Count();
            }

            plotModel.InvalidatePlot(true);
        }

        private void weightsValueChanged (object sender, TextChangedEventArgs e)
        {
            if (!autoUpdate) {
                invalidateCurrentRunValues();
            }
        }

        private void learningCoefficientChanged (object sender, TextChangedEventArgs e)
        {
            invalidateCurrentRunValues();
        }

        private void saveResultsToTextFile (object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.FileName = "wyniki";
            saveFileDialog.Filter = "Plik tekstowy (*.txt)|*.txt";

            if (saveFileDialog.ShowDialog() == true) {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Zestaw uczący L: [");

                for (int i = 0; i < amountOfLearningPoints; i++) {
                    stringBuilder.Append("("+learningSet[i, 0]+","+ learningSet[i, 1] + ")");

                    if (i != amountOfLearningPoints - 1) {
                        stringBuilder.Append(", ");
                    }
                }

                stringBuilder.AppendLine("]");

                stringBuilder.Append(
                    "Epoka".PadRight(10) + "|" +
                    "t".PadRight(10) + "|" +
                    "x0(t)".PadRight(10) + "|" +
                    "x1(t)".PadRight(10) + "|" +
                    "x2(t)".PadRight(10) + "|" +
                    "d(t)".PadRight(10) + "|" +
                    "w0(t)".PadRight(10) + "|" +
                    "w1(t)".PadRight(10) + "|" +
                    "w2(t)".PadRight(10) + "|" +
                    "s(t)".PadRight(10) + "|" +
                    "y(t)".PadRight(10) + "|" +
                    "ok?".PadRight(10) + "\n"
                );

                int iterationCount = 0;
                foreach (SingleIteration singleIteration in iterationHistory) {
                    stringBuilder.Append(
                        ((int)(iterationCount / amountOfLearningPoints)).ToString().PadRight(10) + "|" +
                        iterationCount.ToString().PadRight(10) + "|" +
                        singleIteration.inputs[0].ToString().PadRight(10) + "|" +
                        singleIteration.inputs[1].ToString().PadRight(10) + "|" +
                        singleIteration.inputs[2].ToString().PadRight(10) + "|" +
                        singleIteration.expectedResult.ToString().PadRight(10) + "|" +
                        singleIteration.weights[0].ToString().PadRight(10) + "|" +
                        singleIteration.weights[1].ToString().PadRight(10) + "|" +
                        singleIteration.weights[2].ToString().PadRight(10) + "|" +
                        singleIteration.sumResult.ToString().PadRight(10) + "|" +
                        singleIteration.uniPolarResult.ToString().PadRight(10) + "|" +
                        singleIteration.isOK.ToString().PadRight(10) + "\n"
                    );

                    iterationCount++;
                }

                File.WriteAllText(saveFileDialog.FileName, stringBuilder.ToString());
            }
        }
    }
}
