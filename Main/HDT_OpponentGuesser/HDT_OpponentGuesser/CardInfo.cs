using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDT_OpponentGuesser
{
    public class CardInfo
    {
        // private variables for the cards info: dbfId, name, cost, health, attack, description, type
        private int _dbfId;
        private string _name;
        private int _cost;
        private string _health;
        private string _attack;
        private string _description;
        private string _type;
        private string _rarity;
        private bool _played;
        private string _groups;


        // constructor that takes in the cards info
        public CardInfo(int dbfId, string name, int cost, string health, string attack, string description, string type, string rarity, string groups, bool played)
        {
            _dbfId = dbfId;
            _name = name;
            _cost = cost;
            _health = health;
            _attack = attack;
            _description = description;
            _type = type;
            _played = played;
            _rarity = rarity;
            _groups = groups;
        }



        #region getters for the cards info
        public int GetDbfId()
        {
            return _dbfId;
        }
        public string GetName()
        {
            return _name;
        }
        public int GetCost()
        {
            return _cost;
        }
        public string GetHealth()
        {
            return _health;
        }
        public string GetAttack()
        {
            return _attack;
        }
        public string GetDescription()
        {
            return _description;
        }
        public string GetCardType()
        {
            return _type;
        }
        public bool GetPlayed()
        {
            return _played;
        }
        public string GetRarity()
        {
            return _rarity;
        }
        public string GetGroups()
        {
            return _groups;
        }
        #endregion

    }
}
