﻿using Bot_Dofus_1._29._1.Common.Frames.Transport;
using Bot_Dofus_1._29._1.Common.Network;

/*
    Este archivo es parte del proyecto BotDofus_1.29.1

    BotDofus_1.29.1 Copyright (C) 2019 Alvaro Prendes — Todos los derechos reservados.
    Creado por Alvaro Prendes
    web: http://www.salesprendes.com
*/

namespace Bot_Dofus_1._29._1.Common.Frames.Game
{
    class GameLoginFrame : Frame
    {
        [PacketHandler("M030")]
        public void GetStreamingError(TcpClient prmClient, string prmPacket)
        {
            prmClient.account.logger.log_Error("Login", "Connexion rejetée. Vous n'avez pas pu vous authentifier pour ce serveur car votre connexion a expiré. Assurez-vous de couper les téléchargements, la musique ou les vidéos en continu pour améliorer la qualité et la vitesse de votre connexion.");
            prmClient.account.Disconnect();
        }

        [PacketHandler("M031")]
        public void GetNetworkError(TcpClient prmClient, string prmPacket)
        {
            prmClient.account.logger.log_Error("Login", "Connexion rejetée. Le serveur de jeu n'a pas reçu les informations d'authentification nécessaires après votre identification. Veuillez réessayer et, si le problème persiste, contactez votre administrateur réseau ou votre serveur d'accès Internet. C'est un problème de redirection dû à une mauvaise configuration DNS.");
            prmClient.account.Disconnect();
        }

        [PacketHandler("M032")]
        public void GetFloodConnexionError(TcpClient prmClient, string prmPacket)
        {
            prmClient.account.logger.log_Error("Login", "Pour éviter de déranger les autres joueurs, attendez %1 secondes avant de vous reconnecter.");
            prmClient.account.Disconnect();
        }
    }
}
