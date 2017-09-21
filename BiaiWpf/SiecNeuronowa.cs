using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BiaiWpf
{
    public static class ArrayExt
    {
        //daje wiersz z tablicy dwuwymiarowej
        public static T[] GetRow<T>(this T[,] array, int row)
        {
            if (!typeof(T).IsPrimitive)
                throw new InvalidOperationException("Not supported for managed types.");

            if (array == null)
                throw new ArgumentNullException("array");

            int cols = array.GetUpperBound(1) + 1;
            T[] result = new T[cols];
            int size = Marshal.SizeOf<T>();

            Buffer.BlockCopy(array, row * cols * size, result, 0, cols * size);

            return result;
        }
    }
    public class SiecNeuronowa
    {
        private int iloscWejsc; 
        private int iloscUkrytych; 
        private int iloscWyjsc; 

        private double[] wejscia;
        private double[,] ukryteWejscieWagi;  
        private double[] ukryteBias;
        private double[] ukryteWyjscia;
        private double[,] ukryteWyjsciaWagi; 
        private double[] wyjsciaBias;
        private double[] wyjscia;

        private int Ilosc
        {
            get
            {
                return (iloscWejsc * iloscUkrytych) + (iloscUkrytych * iloscWyjsc) + iloscUkrytych + iloscWyjsc;
            }
        }

        public SiecNeuronowa(int iWejsc, int iUkrytych, int iWyjsc)
        {
            iloscWejsc = iWejsc;
            iloscUkrytych = iUkrytych;
            iloscWyjsc = iWyjsc;
            wejscia = new double[iloscWejsc];
            ukryteWejscieWagi = new double[iloscWejsc, iloscUkrytych];
            ukryteBias = new double[iloscUkrytych];
            ukryteWyjscia = new double[iloscUkrytych];
            ukryteWyjsciaWagi = new double[iloscUkrytych, iloscWyjsc];
            wyjsciaBias = new double[iloscWyjsc];
            wyjscia = new double[iloscWyjsc];
            UstawWstepneWagi();
        }

        private void UstawWstepneWagi()
        {
            double[] wagi = new double[Ilosc];
            //losowanie wstpenych wag
            Random rand = new Random();
            for (int i = 0; i < wagi.Length; ++i)
                wagi[i] = (0.001 - 0.0001) * rand.NextDouble() + 0.0001;

            UstawWagi(wagi);
        }

        public void UstawWagi(double[] weights)
        {
            int index = 0;
            //ustawianie wag
            for (int i = 0; i < iloscWejsc; ++i)
                for (int j = 0; j < iloscUkrytych; ++j)
                    ukryteWejscieWagi[i,j] = weights[index++];
            for (int i = 0; i < iloscUkrytych; ++i)
                ukryteBias[i] = weights[index++];
            for (int i = 0; i < iloscUkrytych; ++i)
                for (int j = 0; j < iloscWyjsc; ++j)
                    ukryteWyjsciaWagi[i,j] = weights[index++];
            for (int i = 0; i < iloscWyjsc; ++i)
                wyjsciaBias[i] = weights[index++];
        }

        public double[] GetWeights()
        {
            double[] result = new double[Ilosc];
            int k = 0;
            foreach (var w in ukryteWejscieWagi)
            {
                result[k++] = w;
            }          
            foreach (var b in ukryteBias)
            {
                result[k++] = b;
            }
            foreach (var w in ukryteWyjsciaWagi)
            {
                result[k++] = w;
            }
            foreach (var b in wyjsciaBias)
            {
                result[k++] = b;
            }
            return result;
        }

        public double[] PoliczWynik(double[] xValues)
        {
            double[] hSums = new double[iloscUkrytych];
            double[] oSums = new double[iloscWyjsc];

            //kopiowanie x do wejsc
            for (int i = 0; i < xValues.Length; ++i)
                wejscia[i] = xValues[i];

            WyliczUkryte(hSums);
            AktywacjaNeuronow(hSums);
            WyliczWyjscia(oSums);

            //softmax
            double sum = 0.0;
            for (int i = 0; i < oSums.Length; ++i)
                sum += Math.Exp(oSums[i]);

            double[] softOut = new double[oSums.Length];
            for (int i = 0; i < oSums.Length; ++i)
                softOut[i] = Math.Exp(oSums[i]) / sum;
            Array.Copy(softOut, wyjscia, softOut.Length);

            double[] retResult = new double[iloscWyjsc];
            Array.Copy(wyjscia, retResult, retResult.Length);
            return retResult;
        }

        private void WyliczUkryte(double[] hSums)
        {
            //liczenie sum wejscie * waga
            for (int j = 0; j < iloscUkrytych; ++j)
                for (int i = 0; i < iloscWejsc; ++i)
                    hSums[j] += wejscia[i] * ukryteWejscieWagi[i, j];

            //dodawanie biasow
            for (int i = 0; i < iloscUkrytych; ++i)
                hSums[i] += ukryteBias[i];
        }

        private void WyliczWyjscia(double[] oSums)
        {
            //obliczanie wyjsx
            for (int j = 0; j < iloscWyjsc; ++j)
                for (int i = 0; i < iloscUkrytych; ++i)
                    oSums[j] += ukryteWyjscia[i] * ukryteWyjsciaWagi[i, j];

            //dodawanie biasow
            for (int i = 0; i < iloscWyjsc; ++i)
                oSums[i] += wyjsciaBias[i];
        }

        private void AktywacjaNeuronow(double[] hSums)
        {
            for (int i = 0; i < iloscUkrytych; ++i)
            {
                if (hSums[i] < -20.0)
                    ukryteWyjscia[i] = -1.0;
                else if (hSums[i] > 20.0)
                    ukryteWyjscia[i] = 1.0;
                else
                    ukryteWyjscia[i] = Math.Tanh(hSums[i]);
            }
        }

        public double[] UczSie(double[,] treningowe, int iloscEpok, double wspolczynnikUczenia, double momentum)
        {
            int epoka = 0;
            double derivative = 0.0;
            double errorSignal = 0.0;

            double[] parametry = new double[iloscWejsc];
            double[] wyniki = new double[iloscWyjsc];
            double[,] hoGrads = new double[iloscUkrytych,iloscWyjsc];  
            double[,] ihGrads = new double[iloscWejsc,iloscUkrytych];   
            double[] oSignals = new double[iloscWyjsc];                 
            double[] hSignals = new double[iloscUkrytych];  

            double[,] ukryteWejsciePoprzedniaDelta = new double[iloscWejsc,iloscUkrytych];
            double[] ukryteBiasPoprzedniaDelta = new double[iloscUkrytych];
            double[,] ukryteWyjsciaPoprzedniaDelta = new double[iloscUkrytych,iloscWyjsc];
            double[] wyjsciaBiasPoprzedniaDelta = new double[iloscWyjsc];


            //normalizacja MinMax
            for (int i = 0; i < treningowe.GetLength(0); i++)
            {
                {
                    double min = 0;
                    double max = 72;
                    double new_min = 0;
                    double new_max = 10;
                    treningowe[i, 1] = (treningowe[i, 2] - min) / (max - min) * (new_max - new_min) + new_min;
                }
                {
                    double min = 0;
                    double max = 72;
                    double new_min = 0;
                    double new_max = 10;
                    treningowe[i, 12] = (treningowe[i, 12] - min) / (max - min) * (new_max - new_min) + new_min;
                }
            }

            int[] liczby = new int[treningowe.GetLength(0)];
            for (int i = 0; i < liczby.Length; ++i)
                liczby[i] = i;

            var file = new System.IO.StreamWriter(@"raport.txt");
            while (epoka < iloscEpok)
            {
                ++epoka;
                double blad = PoliczBlad(treningowe);
                file.WriteLine(string.Format("Epoka {0} \t-\t {1}", epoka, blad));

                Zamien(liczby); 
                for (int i = 0; i < treningowe.GetLength(0); ++i)
                {
                    int x = liczby[i];
                    var wiersz = treningowe.GetRow(x);
                    Array.Copy(wiersz, parametry, iloscWejsc);
                    Array.Copy(wiersz, iloscWejsc, wyniki, 0, iloscWyjsc);
                    PoliczWynik(parametry);

                    for (int k = 0; k < iloscWyjsc; ++k)
                    {
                        errorSignal = wyniki[k] - wyjscia[k];  
                        derivative = (1 - wyjscia[k]) * wyjscia[k]; 
                        oSignals[k] = errorSignal * derivative;
                    }

                    for (int j = 0; j < iloscUkrytych; ++j)
                        for (int k = 0; k < iloscWyjsc; ++k)
                            hoGrads[j,k] = oSignals[k] * ukryteWyjscia[j];

                    for (int j = 0; j < iloscUkrytych; ++j)
                    {
                        derivative = (1 + ukryteWyjscia[j]) * (1 - ukryteWyjscia[j]); 
                        double sum = 0.0; 
                        for (int k = 0; k < iloscWyjsc; ++k)
                        {
                            sum += oSignals[k] * ukryteWyjsciaWagi[j,k];
                        }
                        hSignals[j] = derivative * sum;
                    }

                    for (int k = 0; k < iloscWejsc; ++k)
                        for (int j = 0; j < iloscUkrytych; ++j)
                            ihGrads[k,j] = hSignals[j] * wejscia[k];                   

                    //aktualizacja wag
                    for (int k = 0; k < iloscWejsc; ++k)
                    {
                        for (int j = 0; j < iloscUkrytych; ++j)
                        {
                            double delta = ihGrads[k,j] * wspolczynnikUczenia;                           
                            ukryteWejscieWagi[k,j] += delta + ukryteWejsciePoprzedniaDelta[k,j] * momentum;
                            ukryteWejsciePoprzedniaDelta[k,j] = delta; 
                        }
                    }

                    //aktualizacja ukrytych biasow
                    for (int j = 0; j < iloscUkrytych; ++j)
                    {
                        double delta = hSignals[j] * wspolczynnikUczenia;                       
                        ukryteBias[j] += delta + ukryteBiasPoprzedniaDelta[j] * momentum;
                        ukryteBiasPoprzedniaDelta[j] = delta;
                    }

                    //aktualizacja wag
                    for (int j = 0; j < iloscUkrytych; ++j)
                    {
                        for (int k = 0; k < iloscWyjsc; ++k)
                        {
                            double delta = hoGrads[j,k] * wspolczynnikUczenia;                           
                            ukryteWyjsciaWagi[j,k] += delta + ukryteWyjsciaPoprzedniaDelta[j,k] * momentum;
                            ukryteWyjsciaPoprzedniaDelta[j,k] = delta;
                        }
                    }

                    //aktualizacja biasow
                    for (int k = 0; k < iloscWyjsc; ++k)
                    {
                        double delta = oSignals[k] * wspolczynnikUczenia;                     
                        wyjsciaBias[k] += delta + wyjsciaBiasPoprzedniaDelta[k] * momentum;
                        wyjsciaBiasPoprzedniaDelta[k] = delta;
                    }
                }
            }
            file.Close();
            return GetWeights();
        } 

        private void Zamien(int[] arr) 
        {
            Random rand = new Random();
            for (int i = 0; i < arr.Length; ++i)
            {
                int r = rand.Next(i, arr.Length);
                int tmp = arr[r];
                arr[r] = arr[i];
                arr[i] = tmp;
            }
        } 

        private double PoliczBlad(double[,] treningowe)
        {
            double bladKwadratowySuma = 0.0;
            double[] parametry = new double[iloscWejsc];
            double[] wyniki = new double[iloscWyjsc];

            for (int i = 0; i < treningowe.GetLength(0); ++i)
            {
                var wiersz = treningowe.GetRow(i);
                Array.Copy(wiersz, parametry, iloscWejsc);
                Array.Copy(wiersz, iloscWejsc, wyniki, 0, iloscWyjsc);
                double[] wynikiWyliczone = PoliczWynik(parametry);
                for (int j = 0; j < iloscWyjsc; ++j)
                {
                    double err = wyniki[j] - wynikiWyliczone[j];
                    bladKwadratowySuma += err * err;
                }
            }
            return bladKwadratowySuma / treningowe.Length;
        } 

        public double PoliczDokladnosc(double[,] daneTestowe)
        {
            int liczbaPoprawnych = 0;
            int liczbaZlych = 0;
            double[] parametry = new double[iloscWejsc]; //wejscie
            double[] wyniki = new double[iloscWyjsc]; 
            double[] wynikiPoliczone; 

            for (int i = 0; i < daneTestowe.GetLength(0); ++i)
            {
                var wiersz = daneTestowe.GetRow(i);
                Array.Copy(wiersz, parametry, iloscWejsc); 
                Array.Copy(wiersz, iloscWejsc, wyniki, 0, iloscWyjsc); 
                wynikiPoliczone = PoliczWynik(parametry);
                double maxPoliczone = wynikiPoliczone.Max();
                double max = wyniki.Max();
                int maxIndexPoliczone = wynikiPoliczone.ToList().IndexOf(maxPoliczone);
                int maxIndex = wyniki.ToList().IndexOf(max);

                if (maxIndexPoliczone == maxIndex)
                    ++liczbaPoprawnych;
                else
                    ++liczbaZlych;
            }
            return (liczbaPoprawnych * 1.0) / (liczbaPoprawnych + liczbaZlych);
        }
    }
}
