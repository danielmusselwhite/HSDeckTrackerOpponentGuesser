using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace HDT_OpponentGuesser
{
    // Class to represent cards view in the decklist; taking in on creation: count, name, cost, health, attack, description
    public class CardView : TextBlock
    {
        private string _name;
        private int _cost;
        private string _health;
        private string _attack;
        private int _count;
        private string _description;
        private string _cardType;
        private int _dbfId;
        private string _rarity;
        private string _groups;
        private bool _played;
        public static int cardNumber = 0;
        public static int height = 14;
        public static int spaceBetween = 2;
        Canvas _parent;


        public CardView(string name, int cost, string health, string attack, string description, string type, int dbfId, string rarity, string groups, int count, bool played, Canvas parent)
        {
            // store the variables
            _name = name;
            _cost = cost;
            _health = health;
            _attack = attack;
            _description = description;
            _count = count;
            _cardType = type;
            _dbfId = dbfId;
            _parent = parent;
            _rarity = rarity;
            _groups = groups;


            // create a dict of Type:Color, where: Minion:Orange, Spell:Blue, Secret:Magenta, Weapon:Magenta, Location:Yellow
            Dictionary<string, SolidColorBrush> typeColorDict = new Dictionary<string, SolidColorBrush>();
            // Unplayed cards are vibrant
            if (!played)
            {
                typeColorDict.Add("MINION", Brushes.Maroon);
                typeColorDict.Add("SPELL", Brushes.DarkBlue);
                typeColorDict.Add("SECRET", Brushes.DarkMagenta);
                typeColorDict.Add("WEAPON", Brushes.DarkGoldenrod);
                typeColorDict.Add("LOCATION", Brushes.DarkGreen);
            }
            // Played cards are black (looked too cluttered with different colours for each type)
            else
            {
                typeColorDict.Add("MINION", Brushes.Black);
                typeColorDict.Add("SPELL", Brushes.Black);
                typeColorDict.Add("SECRET", Brushes.Black);
                typeColorDict.Add("WEAPON", Brushes.Black);
                typeColorDict.Add("LOCATION", Brushes.Black);
            }

            // create a new textblock to display the cost, name, and count inside of canvasDeckView
            this.Name = "deckCard" + cardNumber;
            this.Text = UpdateText();
            this.FontSize = 12;
            this.Foreground = Brushes.White;
            try
            {
                this.Background = typeColorDict[type];
            }
            // if the type is not in the dict, set the background to white
            catch (KeyNotFoundException)
            {
                this.Background = Brushes.White;
                this.Foreground = Brushes.Black;
            }
            
            this.FontWeight = FontWeights.Bold;
            this.Height = height;
            this.Width = 280;
            this.TextAlignment = TextAlignment.Left;
            this.TextWrapping = TextWrapping.Wrap;
            this.VerticalAlignment = VerticalAlignment.Top;
            this.Margin = new Thickness(0, 0, 0, 0);
            this.Padding = new Thickness(0, 0, 0, 0);

            // setting the Vertical position of the cards in order based on cardNumber
            this.SetValue(Canvas.BottomProperty, (double)cardNumber * (height + spaceBetween) + spaceBetween);
            this.SetValue(Canvas.LeftProperty, (double)(_parent.Width - this.Width) / 2);


            cardNumber++;

        }

        public static void ResetCounter()
        {
            cardNumber = 0;
        }

        public int GetDbfId()
        {
            return _dbfId;
        }

        private string UpdateText()
        {
            // ternary to also add a star symbol if rarity is "LEGENDARY"
            return _count + "x   |   " + _cost + " Mana   |   " + _name + (_rarity == "LEGENDARY" ? " ★" : "");
        }

        public void ShowCardDetails(Canvas canvas)
        {
            // clear the canvas
            canvas.Children.Clear();

            CardDetailsCanvasPopulator.populateCardDetails(canvas, _name, _cost, _health, _attack, _description, _cardType, _rarity, _groups, this.Background);
        }
    }

}
