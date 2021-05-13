﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocol.Dto.Fight;

namespace GameServer.Fight
{
    /// <summary>
    /// 牌库
    /// </summary>
    public class CardLibrary
    {
        private Queue<CardDto>cardQueue=new Queue<CardDto>();

        public CardLibrary()
        {
            //初始化牌
            InitCard();
            //洗牌
            Shuffle();
        }

        //重置
        public void Init()
        {
            //初始化牌
            InitCard();
            //洗牌
            Shuffle();
        }

        private void InitCard()
        {
            //清空队列
            cardQueue.Clear();

            for (int color = 0; color <= 3; color++)
            {
                for (int weight = 2; weight <= 14; weight++)
                {
                    string cardName = "card_" + color + "_" + weight;
                    CardDto dto = new CardDto(cardName, weight, color);
                    cardQueue.Enqueue(dto);
                }
            }
        }

        /// <summary>
        /// 洗牌
        /// </summary>
        private void Shuffle()
        {
            //转换队列
            List<CardDto> cardList = cardQueue.ToList<CardDto>();

            Random ran = new Random();
            for (int i = 0; i < cardList.Count; i++)
            {
                int ranValue = ran.Next(0, cardList.Count);
                CardDto temp = cardList[i];
                cardList[i] = cardList[ranValue];
                cardList[ranValue] = temp;
            }

            //放回队列里
            cardQueue.Clear();
            foreach (var card in cardList)
            {
                cardQueue.Enqueue(card);
            }
        }

        /// <summary>
        /// 出牌
        /// </summary>
        /// <returns></returns>
        public CardDto DealCard()
        {
            if (cardQueue.Count < 9)
            {
                //初始化牌
                InitCard();
                //洗牌
                Shuffle();
            }
            return cardQueue.Dequeue();
        }

    }
}
