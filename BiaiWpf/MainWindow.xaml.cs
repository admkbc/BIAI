using System;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace BiaiWpf
{

    public partial class MainWindow : Window
    {
        private static float CREDIT_RISK = 0.8f;
        private string plik;
        private double[] inputs;
        private SiecNeuronowa siec;
        public MainWindow()
        {
            InitializeComponent();
            inputs = new double[Konfiguracja.IloscWejsc];
            siec = new SiecNeuronowa(Konfiguracja.IloscWejsc, Konfiguracja.IloscUkrytych, Konfiguracja.IloscWyjsc);         
        }

        double[,] PrzygotujDane()
        {
            double[,] result = new double[Konfiguracja.IloscRekordy, Konfiguracja.IloscWejsc + Konfiguracja.IloscWyjsc];

            var fileLines = System.IO.File.ReadAllLines(plik);
            for (int r = 0; r < fileLines.Length; r++)
            {
                double[] inputs = new double[Konfiguracja.IloscWejsc];
                var line = fileLines[r].Split(',');
                for (int k = Konfiguracja.IloscWyjsc - 1; k < line.Length; k++)
                {
                    inputs[k - 1] = Convert.ToDouble(line[k]);
                }
                double[] outputs = new double[Konfiguracja.IloscWyjsc];
                var o = Convert.ToInt32(line[0]);
                if (o == 1)
                {
                    outputs[0] = 1.0;
                    outputs[1] = 0.0;
                }
                else
                {
                    outputs[0] = 0.0;
                    outputs[1] = 1.0;
                }

                double[] oneOfN = new double[Konfiguracja.IloscWyjsc];

                int maxIndex = 0;
                double maxValue = outputs[0];
                for (int i = 0; i < Konfiguracja.IloscWyjsc; ++i)
                {
                    if (outputs[i] > maxValue)
                    {
                        maxIndex = i;
                        maxValue = outputs[i];
                    }
                }
                oneOfN[maxIndex] = 1.0;

                int c = 0;
                for (int i = 0; i < Konfiguracja.IloscWejsc; ++i) 
                    result[r, c++] = inputs[i];
                for (int i = 0; i < Konfiguracja.IloscWyjsc; ++i) 
                    result[r, c++] = oneOfN[i];
            }
            return result;
        }

        static void PodzielDane(double[,] allData, double trainPct, out double[,] treningowe, out double[,] testowe)
        {
            Random rnd = new Random();
            int totRows = Konfiguracja.IloscRekordy;
            int treningoweIlosc = (int)(totRows * trainPct);
            int testoweIlosc = totRows - treningoweIlosc;
            treningowe = new double[treningoweIlosc, Konfiguracja.IloscWejsc + Konfiguracja.IloscWejsc];
            testowe = new double[testoweIlosc, Konfiguracja.IloscWejsc + Konfiguracja.IloscWejsc];

            double[,] kopia = allData.Clone() as double[,]; 

            for (int i = 0; i < Konfiguracja.IloscRekordy; ++i) 
            {
                int r = rnd.Next(i, Konfiguracja.IloscRekordy);
                double[] tmp = kopia.GetRow(r);
                for (int k = 0; k < Konfiguracja.IloscWejsc + Konfiguracja.IloscWyjsc; k++)
                    kopia[r, k] = kopia[i, k];
                for (int k = 0; k < Konfiguracja.IloscWejsc + Konfiguracja.IloscWyjsc; k++)
                    kopia[i, k] = tmp[k];
            }

            //dzielenie na dane testowe i treningowe
            for (int i = 0; i < treningoweIlosc; ++i)
                for (int k = 0; k < Konfiguracja.IloscWejsc + Konfiguracja.IloscWyjsc; k++)
                    treningowe[i, k] = kopia[i, k];

            for (int i = 0; i < testoweIlosc; ++i)
                for (int k = 0; k < Konfiguracja.IloscWejsc + Konfiguracja.IloscWyjsc; k++)
                    testowe[i, k] = kopia[i + treningoweIlosc, k];
        }

        private void PrzegladajButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text Files (*.txt)|*.txt|Data Files (*.dat)|*.dat|All Files(*.*)|*.*";
            var result = dlg.ShowDialog();
            if (result == true)
            {
                SciezkaTextBox.Text = dlg.FileName;
                plik = dlg.FileName;
            }
        }

        private void UczSiecButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[,] dane = PrzygotujDane();
                double[,] uczace;
                double[,] testowe;
                PodzielDane(dane, 0.80, out uczace, out testowe);
                int maxEpochs = Convert.ToInt32(EpochsTextBox.Text);
                ;
                double learnRate = Convert.ToDouble(LerningRateTextBox.Text);
                double momentum = Convert.ToDouble(MomentumTextBox.Text);
                siec.UczSie(uczace, maxEpochs, learnRate, momentum);
                MessageBox.Show(string.Format("Gotowe! Dokladnosc: {0}", siec.PoliczDokladnosc(testowe)));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błędne parametry uczenia sieci");
            }
        }

        private void SprawdzButton_OnClick(object sender, RoutedEventArgs e)
        {
            WczytajDane();
            try
            {
                var wyjscie = siec.PoliczWynik(inputs);
                var wynik = Convert.ToInt32(Math.Round(wyjscie[0]));
                MessageBox.Show(string.Format("Wynik: {0} ({1:0.00}%)", wynik == 1 ? "Pozytywny" : "Negatywny", wyjscie[0] * 100));
            }
            catch (Exception)
            {
                MessageBox.Show("Błąd sieci");
            }

        }

        private void WczytajDane()
        {
            try
            {
                inputs[0] = AccountBalanceComboBox.SelectedIndex + 1;
                inputs[1] = Convert.ToDouble(DurationTextBox.Text);
                inputs[2] = CreditHistoryComboBox.SelectedIndex;
                inputs[3] = PurposeComboBox.SelectedIndex;
                inputs[4] = Convert.ToDouble(CreditAmountTextBox.Text);
                inputs[5] = SavingAccountComboBox.SelectedIndex + 1;
                inputs[6] = PresentEmploymentComboBox.SelectedIndex + 1;
                inputs[7] = Convert.ToDouble(InstallmentRateTextBox.Text);
                inputs[8] = PersonalStatusComboBox.SelectedIndex + 1;
                inputs[9] = OtherDebtorsComboBox.SelectedIndex + 1;
                inputs[10] = Convert.ToDouble(PresentResidenceTextBox.Text);
                inputs[11] = PropertyComboBox.SelectedIndex + 1;
                inputs[12] = Convert.ToDouble(AgeTextBox.Text);
                inputs[13] = OtherInstallmentComboBox.SelectedIndex + 1;
                inputs[14] = HousingComboBox.SelectedIndex + 1;
                inputs[15] = Convert.ToDouble(NumberCreditsTextBox.Text);
                inputs[16] = JobComboBox.SelectedIndex + 1;
                inputs[17] = Convert.ToDouble(NumberOfPeopleTextBox.Text);
                inputs[18] = TelephoneComboBox.SelectedIndex + 1;
                inputs[19] = ForeignWorkerComboBox.SelectedIndex + 1;
            }
            catch (Exception)
            {

                MessageBox.Show("Wprowadzono błędne dane!");
            }
        }

        private void ZapiszSiecButton_Click(object sender, RoutedEventArgs e)
        {
            double[] weights = siec.GetWeights();

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text Files|*.txt";
            saveFileDialog1.Title = "ZapisanaSiec";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();

                StreamWriter writer = new StreamWriter(fs);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var weight in weights)
                {
                    stringBuilder.Append(weight + ";");

                }
                writer.Write(stringBuilder.ToString());
                writer.Flush();
                stringBuilder.Clear();

                fs.Close();
            }

        }

        private void WczytajSiecButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text Files|*.txt";
            var result = dlg.ShowDialog();
            if (result == true)
            {
                var fileLines = System.IO.File.ReadAllText(dlg.FileName);
                string[] weightsString = fileLines.Split(';');
                double[] weights = new double[weightsString.Length-1];
                for (int i = 0; i < weightsString.Length - 1; i++)
                {
                    weights[i] = Convert.ToDouble(weightsString[i]);
                }
                siec.UstawWagi(weights);
            }

        }

        private void WyliczButton_Click(object sender, RoutedEventArgs e)
        {
            WczytajDane();
            for (int i = 0; i < 30; i++)
            {
                inputs[4] = i;
                var wyjscie = siec.PoliczWynik(inputs);
                if (wyjscie[0] < CREDIT_RISK)
                {
                    MessageBox.Show("Maksymalna kwota pożyczki: "+i*1000+" Marek");
                    break;
                }
            }
        }
    }
}
