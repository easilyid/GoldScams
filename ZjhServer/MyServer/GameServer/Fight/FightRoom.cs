﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyServer;
using Protocol.Constant;
using Protocol.Dto;
using Protocol.Dto.Fight;

namespace GameServer.Fight
{
    /// <summary>
    /// 战斗房间
    /// </summary>
    public class FightRoom
    {
        /// <summary>
        /// 房间ID，唯一标识
        /// </summary>
        public int roomId;
        /// <summary>
        /// 玩家列表
        /// </summary>
        public List<PlayerDto> playerList;
        /// <summary>
        /// 牌库
        /// </summary>
        public CardLibrary CardLibrary;
        /// <summary>
        /// 回合管理类
        /// </summary>
        public RoundModel RoundModel;
        /// <summary>
        /// 离开的玩家列表
        /// </summary>
        public List<int> leaveUserIdList;
        /// <summary>
        /// 弃牌的玩家Id列表
        /// </summary>
        public List<int> GiveUpCardUserIdList;
        /// <summary>
        /// 顶注
        /// </summary>
        public int TopStakes;
        /// <summary>
        /// 底注
        /// </summary>
        public int BottomStakes;
        /// <summary>
        /// 上一位玩家下注的数量
        /// </summary>
        public int LastPlayerStakesCount;
        /// <summary>
        /// 总下注数
        /// </summary>
        public int StakesSum;
        /// <summary>
        /// 庄家在玩家列表中的下标
        /// </summary>
        private int bankerIndex = -1;

        public FightRoom(int roomId, List<ClientPeer> clientList)
        {
            this.roomId = roomId;
            playerList = new List<PlayerDto>();

            foreach (var client in clientList)
            {
                PlayerDto dto = new PlayerDto(client.Id, client.UserName);
                playerList.Add(dto);
            }
            CardLibrary = new CardLibrary();
            RoundModel = new RoundModel();
            leaveUserIdList = new List<int>();
            GiveUpCardUserIdList = new List<int>();
            StakesSum = 0;
        }
        public void Init(List<ClientPeer> clientList)
        {
            StakesSum = 0;
            playerList.Clear();
            foreach (var client in clientList)
            {
                PlayerDto dto = new PlayerDto(client.Id, client.UserName);
                playerList.Add(dto);
            }
        }

        //重置位置，解决下注顺序错乱
        public void ResetPosition(int bankerId)
        {
            // X A B
            if (playerList[0].userId==bankerId)
            {
                //对调1和2的位置
                PlayerDto dto = playerList[1];
                playerList[1] = playerList[2];
                playerList[2] = dto;
            }
            // A X B
            if (playerList[1].userId==bankerId)
            {
                PlayerDto dto = playerList[0];
                playerList[0] = playerList[2];
                playerList[2] = dto;

            }
            // A B X
            if (playerList[2].userId == bankerId)
            {
                PlayerDto dto = playerList[0];
                playerList[0] = playerList[1];
                playerList[1] = dto;

            }

        }

        /// <summary>
        /// 销毁房间，重置房间数据
        /// </summary>
        public void Destory()
        {
            playerList.Clear();
            CardLibrary.Init();
            RoundModel.Init();
            leaveUserIdList.Clear();
            GiveUpCardUserIdList.Clear();
            StakesSum = 0;
            bankerIndex = -1;
        }

        /// <summary>
        /// 广播发消息
        /// </summary>
        public void Broadcast(int opCode, int subCode, object value, ClientPeer exceptClient = null)
        {
            NetMsg msg = new NetMsg(opCode, subCode, value);
            byte[] data = EncodeTool.EncodeMsg(msg);
            byte[] packet = EncodeTool.EncodePacket(data);
            foreach (var player in playerList)
            {
                //获取到客户端连接对象
                ClientPeer client = DataBase.DatabaseManager.GetClientPeerByUserId(player.userId);
                if (client == exceptClient)
                    continue;
                client.SendMsg(packet);
            }
        }
        /// <summary>
        /// 是否离开房间
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public bool IsLeaveRoom(int userid)
        {
            return leaveUserIdList.Contains(userid);
        }
        /// <summary>
        /// 是否弃牌
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public bool IsGiveUpCard(int userid)
        {
            return GiveUpCardUserIdList.Contains(userid);
        }
        /// <summary>
        /// 轮换下注
        /// </summary>
        /// <returns>下一次下注的玩家ID</returns>
        public int Turn()
        {
            int currentUserId = RoundModel.CurrentStakesUserId;
            int nextUserId = GetNextUserId(currentUserId);
            RoundModel.Turn(nextUserId);
            return nextUserId;
        }
        /// <summary>
        /// 获得下一次下注的玩家ID
        /// </summary>
        /// <param name="currentId"></param>
        /// <returns></returns>
        private int GetNextUserId(int currentId)
        {
            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i].userId == currentId)
                {
                    //i==2 就是0下一位的玩家
                    if (i == playerList.Count - 1)
                    {
                        return playerList[0].userId;
                    }
                    else
                        return playerList[i + 1].userId;
                }
            }
            return -1;
        }
        /// <summary>
        /// 更新玩家的下注总数
        /// </summary>
        public int UpdatePlayerStakesSum(int userId, int stakesCount)
        {
            foreach (var player in playerList)
            {
                //把当前的数量加入到这个数量
                if (player.userId == userId)
                {
                    player.stakesSum += stakesCount;
                    return player.stakesSum;
                }
            }
            return 0;
        }

        /// <summary>
        /// 选择庄家
        /// </summary>
        public ClientPeer SetBanker()
        {
            Random ran = new Random();
            int ranIndex = ran.Next(0, playerList.Count);
            bankerIndex = ranIndex;
            //随机到的庄家用户ID
            int userId = playerList[ranIndex].userId;
            playerList[ranIndex].identity = Identity.Banker;
            //设置庄家为默认下注者
            RoundModel.Start(userId);
            ClientPeer bankerClient = DataBase.DatabaseManager.GetClientPeerByUserId(userId);
            string userName = bankerClient.UserName;
            Console.WriteLine("庄家为：" + userName);
            return bankerClient;
        }
        /// <summary>
        /// 发牌
        /// </summary>
        public void DealCards()
        {
            for (int i = 0; i < 9; i++)
            {
                playerList[bankerIndex].AddCard(CardLibrary.DealCard());
                bankerIndex++;
                if (bankerIndex > playerList.Count - 1)
                {
                    bankerIndex = 0;
                }
            }
        }
        /// <summary>
        /// 对牌排序
        /// </summary>
        /// <param name="cardList"></param>
        private void SortCard(ref List<CardDto> cardList)
        {
            for (int i = 0; i < cardList.Count - 1; i++)
            {
                for (int j = 0; j < cardList.Count - 1 - i; j++)
                {
                    if (cardList[j].Weight < cardList[j + 1].Weight)
                    {
                        CardDto temp = cardList[j];
                        cardList[j] = cardList[j + 1];
                        cardList[j + 1] = temp;
                    }
                }
            }
        }
        /// <summary>
        /// 对房间内的所有玩家手牌排序
        /// </summary>
        public void SortAllPlayerCard()
        {
            foreach (var player in playerList)
            {
                SortCard(ref player.cardList);
            }
        }
        /// <summary>
        /// 获取所有玩家牌型
        /// </summary>
        public void GetAllPlayerCardType()
        {
            foreach (var player in playerList)
            {
                player.cardType = GetCardType(player.cardList);
            }
        }
        /// <summary>
        /// 获取牌型
        /// </summary>
        /// <param name="cardList"></param>
        private CardType GetCardType(List<CardDto> cardList)
        {
            CardType temp = CardType.None;
            //532
            if (cardList[0].Weight == 5 && cardList[1].Weight == 3 && cardList[2].Weight == 2)
            {
                temp = CardType.Max;
            }
            //666
            else if (cardList[0].Weight == cardList[1].Weight && cardList[0].Weight == cardList[2].Weight)
            {
                temp = CardType.Baozi;
            }
            //765 颜色也一样
            else if (cardList[0].Color == cardList[1].Color && cardList[0].Color == cardList[2].Color &&
                cardList[0].Weight == cardList[1].Weight + 1 && cardList[0].Weight == cardList[2].Weight + 2)
            {
                temp = CardType.Shunjin;
            }
            //颜色一样
            else if (cardList[0].Color == cardList[1].Color && cardList[0].Color == cardList[2].Color)
            {
                temp = CardType.Jinhua;
            }
            //765
            else if (cardList[0].Weight == cardList[1].Weight + 1 && cardList[0].Weight == cardList[2].Weight + 2)
            {
                temp = CardType.Shunzi;
            }
            //665 688
            else if (cardList[0].Weight == cardList[1].Weight || cardList[1].Weight == cardList[2].Weight)
            {
                temp = CardType.Duizi;
            }
            else
            {
                temp = CardType.Min;
            }

            return temp;
        }

    }
}
