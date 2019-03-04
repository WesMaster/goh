﻿using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using static MoneyGrip.Data.Enums;
using System;

namespace MoneyGrip.Models
{
    using dataDict = ConcurrentDictionary<string, int>;

    public class Begroting
    {
        public Inkomst[] inkomsten { get; set; }
        public Contract[] contracten { get; set; }
        public Budget[] budgetten { get; set; }
        public Reservering[] reserveringen { get; set; }
        public Afschrijving[] afschrijvingen { get; set; }
        public Spaardoel[] spaardoelen { get; set; }

        public dataDict resultatenPerMaand { get; set; }

        public int jaar { get; set; }
        private readonly DateTimeFormatInfo ci = CultureInfo.CreateSpecificCulture("nl").DateTimeFormat;
        readonly private ConcurrentDictionary<string, object> overview = new ConcurrentDictionary<string, object>();

        public string calculate()
        {
            dataDict inkomstenPerMaand = new dataDict();
            dataDict contractenPerMaand = new dataDict();
            dataDict budgettenPerMaand = new dataDict();
            dataDict reserveringenPerMaand = new dataDict();
            dataDict afschrijvingenPerMaand = new dataDict();

            if (inkomsten != null)
            {
                inkomstenPerMaand = splitToMonths(inkomsten);
            }
            if (contracten != null)
            {
                contractenPerMaand = splitToMonths(contracten);
            }
            if (budgetten != null)
            {
                budgettenPerMaand = splitToMonths(budgetten);
            }
            if (reserveringen != null)
            {
                reserveringenPerMaand = splitToMonths(reserveringen);
            }
            if (afschrijvingen != null)
            {
                afschrijvingenPerMaand = splitToMonths(afschrijvingen);
            }

            dataDict uitgavenPerMaand = new dataDict();
            uitgavenPerMaand = mergeDicts(uitgavenPerMaand, contractenPerMaand);
            uitgavenPerMaand = mergeDicts(uitgavenPerMaand, budgettenPerMaand);
            uitgavenPerMaand = mergeDicts(uitgavenPerMaand, reserveringenPerMaand);
            uitgavenPerMaand = mergeDicts(uitgavenPerMaand, afschrijvingenPerMaand);

            overview.TryAdd("inkomsten", inkomstenPerMaand);
            overview.TryAdd("contracten", contractenPerMaand);
            overview.TryAdd("budgetten", budgettenPerMaand);
            overview.TryAdd("reserveringen", reserveringenPerMaand);
            overview.TryAdd("afschrijvingen", afschrijvingenPerMaand);
            overview.TryAdd("uitgaven", uitgavenPerMaand);

            resultatenPerMaand = berekenResultaat();
            overview.TryAdd("resultaat", resultatenPerMaand);

            ConcurrentDictionary<string, dataDict> spaardoelenPerMaand = new ConcurrentDictionary<string, dataDict>();
            if (spaardoelen != null)
            {
                spaardoelenPerMaand = divideOverMonths(spaardoelen, resultatenPerMaand);
            }
            overview.TryAdd("spaardoelen", spaardoelenPerMaand);

            return JsonConvert.SerializeObject(overview);
        }

        private dataDict mergeDicts(dataDict baseDict, dataDict toMergeDict)
        {
            foreach (var kvp in toMergeDict)
            {
                baseDict.AddOrUpdate(kvp.Key, kvp.Value, (key, value) => value + kvp.Value);
            }

            return baseDict;
        }

        private ConcurrentDictionary<string, int> splitToMonths(BedragPerPeriode[] bedragen)
        {
            ConcurrentDictionary<string, int> data = new ConcurrentDictionary<string, int>();

            foreach (BedragPerPeriode bedrag in bedragen)
            {
                int maandFactor = 1;
                switch (bedrag.Interval)
                {
                    case Interval.Kwartaal:
                        maandFactor = 3;
                        break;
                    case Interval.Jaar:
                        maandFactor = 12;
                        break;
                    default:
                        break;
                }

                int beginmaand = getBeginMaand(bedrag.Begindatum);
                int eindmaand = getEindMaand(bedrag.Einddatum);

                for (int i = beginmaand; i <= eindmaand; i++)
                {
                    int resultaatBedrag = 0;
                    if (i == beginmaand && maandFactor != 1)
                    {
                        int maandbedrag = bedrag.Bedrag * 100 / maandFactor;
                        int aantalMaandenTeVerrekenen = 1;
                        if (beginmaand % maandFactor > 0)
                        {
                            aantalMaandenTeVerrekenen = Math.Min(maandFactor, eindmaand) - (beginmaand % maandFactor) + 1;
                        }

                        resultaatBedrag = (maandbedrag * aantalMaandenTeVerrekenen + 50) / 100;
                    }
                    if (i % maandFactor == 0 && i != eindmaand)
                    {
                        int maandbedrag = bedrag.Bedrag * 100 / maandFactor;
                        int aantalMaandenTeVerrekenen = maandFactor;
                        if (eindmaand - i < maandFactor)
                        {
                            aantalMaandenTeVerrekenen = eindmaand - i;
                        }

                        resultaatBedrag += (maandbedrag * aantalMaandenTeVerrekenen + 50) / 100;
                    }
                    else if (i == eindmaand && maandFactor == 1)
                    {
                        resultaatBedrag = bedrag.Bedrag;
                    }

                    if (resultaatBedrag > 0)
                    {
                        data.AddOrUpdate("totaal", resultaatBedrag, (key, oldValue) => oldValue + resultaatBedrag);
                        data.AddOrUpdate(ci.GetMonthName(i), resultaatBedrag, (key, oldValue) => oldValue + resultaatBedrag);
                    }
                }
            }
            return data;
        }

        private ConcurrentDictionary<string, int> splitToMonths(Reservering[] reserveringen)
        {
            ConcurrentDictionary<string, int> data = new ConcurrentDictionary<string, int>();

            foreach (Reservering reservering in reserveringen)
            {
                if (reservering.Maand == 0)
                {
                    for (int i = 1; i <= 12; i++)
                    {
                        data.AddOrUpdate("totaal", reservering.Bedrag, (key, oldValue) => oldValue + reservering.Bedrag);
                        data.AddOrUpdate(ci.GetMonthName(i), reservering.Bedrag, (key, oldValue) => oldValue + reservering.Bedrag);
                    }
                }
                else
                {
                    data.AddOrUpdate("totaal", reservering.Bedrag, (key, oldValue) => oldValue + reservering.Bedrag);
                    data.AddOrUpdate(ci.GetMonthName(reservering.Maand), reservering.Bedrag, (key, oldValue) => oldValue + reservering.Bedrag);
                }
            }

            return data;
        }

        private ConcurrentDictionary<string, int> splitToMonths(Afschrijving[] afschrijvingen)
        {
            ConcurrentDictionary<string, int> data = new ConcurrentDictionary<string, int>();
            AfschrijvingCalcModel calc = new AfschrijvingCalcModel();
            calc.afschrijvingen = afschrijvingen;

            for (int i = 1; i <= 12; i++)
            {
                int bedrag = calc.getTotaleWaarde(i, jaar);
                if (bedrag > 0)
                {
                    data.AddOrUpdate("totaal", bedrag, (key, oldValue) => oldValue + bedrag);
                    data.AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);
                }
            }

            return data;
        }

        private ConcurrentDictionary<string, int> berekenResultaat()
        {
            ConcurrentDictionary<string, int> data = new ConcurrentDictionary<string, int>();
            int jaarResultaat = 0;
            for (int i = 1; i <= 12; i++)
            {
                int bedrag = 0;
                bedrag += getMaandUitgave("inkomsten", i);
                bedrag -= getMaandUitgave("contracten", i);
                bedrag -= getMaandUitgave("budgetten", i);
                bedrag -= getMaandUitgave("reserveringen", i);
                bedrag -= getMaandUitgave("afschrijvingen", i);

                data.AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);

                jaarResultaat += bedrag;
            }

            data.AddOrUpdate("totaal", jaarResultaat, (key, oldValue) => oldValue + jaarResultaat);

            return data;
        }

        private int getMaandUitgave(string type, int maand)
        {
            int maandUitgave = 0;
            if (overview.TryGetValue(type, out object uitgaveObject))
            {
                ConcurrentDictionary<string, int> uitgave = uitgaveObject as ConcurrentDictionary<string, int>;
                uitgave.TryGetValue(ci.GetMonthName(maand), out maandUitgave);
            }
            return maandUitgave;
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, int>> divideOverMonths(Spaardoel[] spaardoelen, ConcurrentDictionary<string, int> resultatenPerMaand)
        {
            ConcurrentDictionary<string, ConcurrentDictionary<string, int>> data = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            data.GetOrAdd("Totaal", new ConcurrentDictionary<string, int>());
            data.GetOrAdd("Eindbedrag", new ConcurrentDictionary<string, int>());

            foreach (Spaardoel spaardoel in spaardoelen)
            {
                string label = spaardoel.LabelNavigation.Naam;
                int beginmaand = spaardoel.EersteMaand;
                int eindmaand = spaardoel.LaatsteMaand;
                int eindbedrag = spaardoel.Eindbedrag ?? 0;

                data.GetOrAdd(label, new ConcurrentDictionary<string, int>());
                if (spaardoel.Percentage == null)
                {
                    int resterendEindBedrag = spaardoel.Eindbedrag.Value;
                    for (int i = eindmaand; i >= beginmaand; i--)
                    {
                        int resultaat = 0;
                        resultatenPerMaand.TryGetValue(ci.GetMonthName(i), out resultaat);

                        int huidigBedragInMaand = 0;
                        data["Totaal"].TryGetValue(ci.GetMonthName(i), out huidigBedragInMaand);

                        int bedrag = Math.Min(Math.Max(resultaat - huidigBedragInMaand, 0), resterendEindBedrag);
                        data[label].AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);
                        data[label].AddOrUpdate("totaal", bedrag, (key, oldValue) => oldValue + bedrag);

                        data["Totaal"].AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);
                        data["Totaal"].AddOrUpdate("totaal", bedrag, (key, oldValue) => oldValue + bedrag);
                        data["Eindbedrag"].AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);

                        resterendEindBedrag -= bedrag;
                    }
                }
                else
                {
                    int totaalBedrag = 0;
                    for (int i = beginmaand; i <= eindmaand; i++)
                    {
                        resultatenPerMaand.TryGetValue(ci.GetMonthName(i), out int resultaat);

                        data["Totaal"].TryGetValue(ci.GetMonthName(i), out int huidigBedragInMaand);

                        data["Eindbedrag"].TryGetValue(ci.GetMonthName(i), out int totaalVast);
                      
                        int bedrag = (int)(Math.Max(resultaat - totaalVast, 0) * ((decimal)spaardoel.Percentage / 100));

                        if(spaardoel.Eindbedrag != null)
                        {
                            bedrag = Math.Min(bedrag, (int)spaardoel.Eindbedrag - totaalBedrag);
                        }
                        totaalBedrag += bedrag;

                        if(resultaat - huidigBedragInMaand - bedrag < 5)
                        {
                            bedrag = resultaat - huidigBedragInMaand;
                        }

                        data[label].AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);
                        data[label].AddOrUpdate("totaal", bedrag, (key, oldValue) => oldValue + bedrag);

                        data["Totaal"].AddOrUpdate(ci.GetMonthName(i), bedrag, (key, oldValue) => oldValue + bedrag);
                        data["Totaal"].AddOrUpdate("totaal", bedrag, (key, oldValue) => oldValue + bedrag);
                    }
                }
                data[label].AddOrUpdate("eindbedrag", eindbedrag, (key, oldValue) => eindbedrag);
            }

            ConcurrentDictionary<string, int> eindBedragVerwijderd = null;
            data.TryRemove("Eindbedrag", out eindBedragVerwijderd);
            return data;
        }

        private int getBeginMaand(DateTime beginDatum)
        {
            int beginmaand = 1;
            if (beginDatum.Year == jaar)
            {
                beginmaand = beginDatum.Month;
            }

            return beginmaand;
        }

        private int getEindMaand(DateTime? eindDatum)
        {
            int eindmaand = 12;
            if (eindDatum.HasValue && eindDatum.Value.Year == jaar)
            {
                eindmaand = eindDatum.Value.Month;
            }

            return eindmaand;
        }
    }
}
