using System;
using System.Collections.Generic;

namespace BoekWinkel
{
    public enum Verschijningsperiode
    {
        Dagelijks,
        Wekelijks,
        Maandelijks
    }

    public class Boek
    {
        private string _isbn;
        private string _naam;
        private string _uitgever;
        private decimal _prijs;

        public string Isbn 
        { 
            get => _isbn; 
            set => _isbn = value; 
        }
        
        public string Naam 
        { 
            get => _naam; 
            set => _naam = value; 
        }
        
        public string Uitgever 
        { 
            get => _uitgever; 
            set => _uitgever = value; 
        }
        
        public decimal Prijs 
        { 
            get => _prijs; 
            set => _prijs = value < 5 ? 5 : (value > 50 ? 50 : value); 
        }

        public Boek(string isbn, string naam, string uitgever, decimal prijs)
        {
            Isbn = isbn;
            Naam = naam;
            Uitgever = uitgever;
            Prijs = prijs;
        }

        public Boek() : this("", "", "", 5) { }

        public virtual void Lees()
        {
            Console.Write("ISBN: ");
            Isbn = Console.ReadLine() ?? "";
            
            Console.Write("Naam: ");
            Naam = Console.ReadLine() ?? "";
            
            Console.Write("Uitgever: ");
            Uitgever = Console.ReadLine() ?? "";
            
            Console.Write("Prijs (€): ");
            if (decimal.TryParse(Console.ReadLine(), out decimal prijs))
                Prijs = prijs;
            else
                Prijs = 5;
        }

        public override string ToString()
        {
            return $"ISBN: {Isbn}, Naam: {Naam}, Uitgever: {Uitgever}, Prijs: {Prijs:C}";
        }
    }

    public class Tijdschrift : Boek
    {
        public Verschijningsperiode Periode { get; set; }

        public Tijdschrift() : base()
        {
            Periode = Verschijningsperiode.Maandelijks;
        }

        public Tijdschrift(string isbn, string naam, string uitgever, decimal prijs, 
            Verschijningsperiode periode) : base(isbn, naam, uitgever, prijs)
        {
            Periode = periode;
        }

        public override void Lees()
        {
            base.Lees();
            
            Console.WriteLine("Periodiciteit (0=Dagelijks, 1=Wekelijks, 2=Maandelijks): ");
            if (Enum.TryParse(Console.ReadLine(), out Verschijningsperiode periode))
                Periode = periode;
            else
                Periode = Verschijningsperiode.Maandelijks;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, Periodiciteit: {Periode}";
        }
    }

    public class Bestelling<T> where T : Boek
    {
        private static int _lastId = 0;
        private int _id;

        public int Id 
        { 
            get => _id;
            private set => _id = value;
        }
        
        public T Item { get; set; }
        public DateTime Datum { get; set; }
        public int Aantal { get; set; }
        public int? AbonnementsPeriodeInMaanden { get; set; }

        public event EventHandler<BestellingEventArgs> BestellingGeplaatst;

        public Bestelling(T item, int aantal, int? abonnementsPeriodeInMaanden = null)
        {
            Id = ++_lastId;
            Item = item;
            Datum = DateTime.Now;
            Aantal = aantal;
            AbonnementsPeriodeInMaanden = abonnementsPeriodeInMaanden;
        }

        public Tuple<string, int, decimal> Bestel()
        {
            decimal totaalPrijs = Item.Prijs * Aantal;
            
            if (Item is Tijdschrift && AbonnementsPeriodeInMaanden.HasValue)
            {
                int uitgavesPerMaand = Item is Tijdschrift tijdschrift
                    ? tijdschrift.Periode switch
                    {
                        Verschijningsperiode.Dagelijks => 30,
                        Verschijningsperiode.Wekelijks => 4,
                        Verschijningsperiode.Maandelijks => 1,
                        _ => 1
                    }
                    : 1;
                    
                totaalPrijs = Item.Prijs * uitgavesPerMaand * AbonnementsPeriodeInMaanden.Value;
            }

            OnBestellingGeplaatst(new BestellingEventArgs(Item.Naam, Aantal, totaalPrijs));

            return new Tuple<string, int, decimal>(Item.Isbn, Aantal, totaalPrijs);
        }

        protected virtual void OnBestellingGeplaatst(BestellingEventArgs e)
        {
            BestellingGeplaatst?.Invoke(this, e);
        }

        public override string ToString()
        {
            string result = $"Bestelling #{Id} van {Datum:dd/MM/yyyy}, {Aantal} exempla(a)r(en) van:\n{Item}";
            
            if (Item is Tijdschrift && AbonnementsPeriodeInMaanden.HasValue)
                result += $"\nAbonnement voor {AbonnementsPeriodeInMaanden} maanden";
                
            return result;
        }
    }

    public class BestellingEventArgs : EventArgs
    {
        public string ProductNaam { get; }
        public int Aantal { get; }
        public decimal TotaalPrijs { get; }

        public BestellingEventArgs(string productNaam, int aantal, decimal totaalPrijs)
        {
            ProductNaam = productNaam;
            Aantal = aantal;
            TotaalPrijs = totaalPrijs;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("===== Boekwinkel Bestellingssysteem =====");

            
            Boek boek1 = new Boek("978-0-306-40615-7", "De Kleine Prins", "Uitgeverij J.M. Meulenhoff", 12.99m);
            Boek boek2 = new Boek("978-3-16-148410-0", "Honderd jaar eenzaamheid", "De Geus", 18.50m);

            
            Tijdschrift tijdschrift1 = new Tijdschrift("977-1234-56789", "Wetenschap & Leven", "Sanoma Media", 6.95m, Verschijningsperiode.Maandelijks);
            Tijdschrift tijdschrift2 = new Tijdschrift("977-9876-54321", "De Volkskrant", "DPG Media", 2.50m, Verschijningsperiode.Dagelijks);

            EventHandler<BestellingEventArgs> BestellingHandler = (sender, e) =>
            {
                Console.WriteLine($"\n=== BESTELLINGSBEVESTIGING ===");
                Console.WriteLine($"Product: {e.ProductNaam}");
                Console.WriteLine($"Aantal: {e.Aantal}");
                Console.WriteLine($"Totaalprijs: {e.TotaalPrijs:C}");
                Console.WriteLine($"=============================\n");
            };

            bool doorgaan = true;
            List<Boek> catalogus = new List<Boek> { boek1, boek2, tijdschrift1, tijdschrift2 };
            List<object> bestellingen = new List<object>();

            while (doorgaan)
            {
                Console.WriteLine("\nHoofdmenu:");
                Console.WriteLine("1. Catalogus weergeven");
                Console.WriteLine("2. Nieuw boek toevoegen");
                Console.WriteLine("3. Nieuw tijdschrift toevoegen");
                Console.WriteLine("4. Bestelling plaatsen");
                Console.WriteLine("5. Bestellingen weergeven");
                Console.WriteLine("0. Afsluiten");
                
                Console.Write("\nUw keuze: ");
                string keuze = Console.ReadLine() ?? "";

                switch (keuze)
                {
                    case "1": 
                        Console.WriteLine("\n=== CATALOGUS ===");
                        for (int i = 0; i < catalogus.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {catalogus[i]}");
                        }
                        break;

                    case "2": 
                        Boek nieuwBoek = new Boek();
                        Console.WriteLine("\nVoer de details van het nieuwe boek in:");
                        nieuwBoek.Lees();
                        catalogus.Add(nieuwBoek);
                        Console.WriteLine("Boek succesvol toegevoegd!");
                        break;

                    case "3": 
                        Tijdschrift nieuwTijdschrift = new Tijdschrift();
                        Console.WriteLine("\nVoer de details van het nieuwe tijdschrift in:");
                        nieuwTijdschrift.Lees();
                        catalogus.Add(nieuwTijdschrift);
                        Console.WriteLine("Tijdschrift succesvol toegevoegd!");
                        break;

                    case "4": 
                        Console.WriteLine("\n=== NIEUWE BESTELLING ===");
                        Console.WriteLine("Kies een product uit de catalogus (nummer):");
                        for (int i = 0; i < catalogus.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {catalogus[i].Naam} - {catalogus[i].Prijs:C}");
                        }

                        if (int.TryParse(Console.ReadLine(), out int index) && index >= 1 && index <= catalogus.Count)
                        {
                            Boek geselecteerdItem = catalogus[index - 1];

                            Console.Write("Aantal: ");
                            if (int.TryParse(Console.ReadLine(), out int aantal) && aantal > 0)
                            {
                                int? periodeInMaanden = null;

                                if (geselecteerdItem is Tijdschrift)
                                {
                                    Console.Write("Is dit een abonnement? (J/N): ");
                                    if ((Console.ReadLine() ?? "").ToUpper() == "J")
                                    {
                                        Console.Write("Abonnementsduur in maanden: ");
                                        if (int.TryParse(Console.ReadLine(), out int periode) && periode > 0)
                                            periodeInMaanden = periode;
                                    }
                                }

                                if (geselecteerdItem is Tijdschrift tijdschrift)
                                {
                                    var bestelling = new Bestelling<Tijdschrift>(tijdschrift, aantal, periodeInMaanden);
                                    bestelling.BestellingGeplaatst += BestellingHandler;
                                    var result = bestelling.Bestel();
                                    bestellingen.Add(bestelling);
                                }
                                else
                                {
                                    var bestelling = new Bestelling<Boek>(geselecteerdItem, aantal);
                                    bestelling.BestellingGeplaatst += BestellingHandler;
                                    var result = bestelling.Bestel();
                                    bestellingen.Add(bestelling);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ongeldig aantal.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ongeldige selectie.");
                        }
                        break;

                    case "5":
                        Console.WriteLine("\n=== GEPLAATSTE BESTELLINGEN ===");
                        if (bestellingen.Count == 0)
                        {
                            Console.WriteLine("Er zijn nog geen bestellingen geplaatst.");
                        }
                        else
                        {
                            foreach (var bestelling in bestellingen)
                            {
                                Console.WriteLine(bestelling.ToString());
                                Console.WriteLine("-------------------------");
                            }
                        }
                        break;

                    case "0":
                        doorgaan = false;
                        break;

                    default:
                        Console.WriteLine("Ongeldige optie, probeer opnieuw.");
                        break;
                }
            }

            Console.WriteLine("\nBedankt voor het gebruik van ons bestellingssysteem. Tot ziens!");
        }
    }
}
