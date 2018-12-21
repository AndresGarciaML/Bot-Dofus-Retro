﻿using System;
using System.Collections.Generic;
using System.Text;
using Bot_Dofus_1._29._1.Protocolo.Extensiones;
using Bot_Dofus_1._29._1.Utilidades.Criptografia;

/*
    Este archivo es parte del proyecto BotDofus_1.29.1

    BotDofus_1.29.1 Copyright (C) 2018 Alvaro Prendes — Todos los derechos reservados.
    Creado por Alvaro Prendes
    web: http://www.salesprendes.com
*/

namespace Bot_Dofus_1._29._1.Otros.Mapas.Movimiento
{
    internal class Pathfinding
    {
        public List<int> lista_abierta = new List<int>();
        public List<int> lista_cerrada = new List<int>();
        private readonly int[] Plist;
        private readonly int[] Flist;
        private readonly int[] Glist;
        private readonly int[] Hlist;
        private Mapa mapa;

        private bool es_pelea;
        private int nombreDePM;

        public Pathfinding(Mapa _mapa)
        {
            mapa = _mapa;
            Plist = new int[1025];
            Flist = new int[1025];
            Glist = new int[1025];
            Hlist = new int[1025];
        }

        public static string get_Direccion_Char(int direccion)
        {
            if (direccion >= Hash.caracteres_array.Length)
                return string.Empty;
            return Hash.caracteres_array[direccion].ToString();
        }

        private void cargar_Obstaculos()
        {
            for (int i = 0; i < mapa.celdas.Length - 1; i++)
            {
                if (mapa.celdas[i].tipo < (TipoCelda)4)
                {
                    lista_cerrada.Add(i);
                }
                if (mapa.celdas[i].object2Movement)
                {
                    lista_cerrada.Add(i);
                }
            }
        }

        public string pathing(int celda_actual, int celda_final)
        {
            try
            {
                cargar_Obstaculos();
                lista_cerrada.Remove(celda_final);
                return get_Pathfinding_Limpio(get_Pathfinding(celda_actual, celda_final));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public string pathing(int celda_actual, int celda_final, bool _es_pelea, int _nombre_pm)
        {
            try
            {
                cargar_Obstaculos();
                lista_cerrada.Remove(celda_final);

                es_pelea = _es_pelea;
                nombreDePM = _nombre_pm;
                return get_Pathfinding_Limpio(get_Pathfinding(celda_actual, celda_final));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string get_Pathfinding(int celda_1, int celda_2)
        {
            int actual;
            lista_abierta.Add(celda_1);

            while (!lista_abierta.Contains(celda_2))
            {
                actual = get_F_Punto();
                if (actual != celda_2)
                {
                    lista_cerrada.Add(actual);
                    lista_abierta.Remove(actual);

                    get_Hijo(actual).ForEach(celda =>
                    {
                        if (!lista_cerrada.Contains(celda))
                        {
                            if (lista_abierta.Contains(celda))
                            {
                                if (Glist[actual] + 5 < Glist[celda])
                                {
                                    Plist[celda] = actual;
                                    Glist[celda] = Glist[actual] + 5;
                                    Hlist[celda] = get_Distancia_Estimada(celda, celda_2);
                                    Flist[celda] = Glist[celda] + Hlist[celda];
                                }
                            }
                            else
                            {
                                lista_abierta.Add(celda);
                                lista_abierta[lista_abierta.Count - 1] = celda;
                                Glist[celda] = Glist[actual] + 5;
                                Hlist[celda] = get_Distancia_Estimada(celda, celda_2);
                                Flist[celda] = Glist[celda] + Hlist[celda];
                                Plist[celda] = actual;
                            }
                        }
                    });
                }
                if (lista_cerrada.Count > 999)
                    throw new Exception("El camino es impossible");
            }
            return get_Padre(celda_1, celda_2);
        }

        private string get_Padre(int cell1, int cell2)
        {
            int actual = cell2;
            List<int> pathCell = new List<int>();
            pathCell.Add(actual);

            while (actual != cell1)
            {
                pathCell.Add(Plist[actual]);
                actual = Plist[actual];
            }
            return getPath(pathCell);
        }

        private string getPath(List<int> camino_celda)
        {
            camino_celda.Reverse();
            StringBuilder pathing = new StringBuilder();
            int actual, hijo, pm_usados = 0;
            for (int i = 0; i < camino_celda.Count - 1; i++)
            {
                pm_usados += 1;
                if (pm_usados > nombreDePM && es_pelea)
                    return pathing.ToString();
                actual = camino_celda[i];
                hijo = camino_celda[i + 1];
                pathing.Append(get_Direccion_Char(get_Orientacion_Casilla(actual, hijo))).Append(get_Celda_Char(hijo));
            }
            return pathing.ToString();
        }

        public static string get_Celda_Char(int celda)
        {
            int CharCode2 = celda % Hash.caracteres_array.Length;
            int CharCode1 = (celda - CharCode2) / Hash.caracteres_array.Length;
            return Hash.caracteres_array[CharCode1].ToString() + Hash.caracteres_array[CharCode2].ToString();
        }

        private List<int> get_Hijo(int celda_id)
        {
            int x = get_Celda_X_Coordenadas(celda_id), y = get_Celda_Y_Coordenadas(celda_id);
            int temporal, x_temporal, y_temporal;
            List<int> lista_hijo = new List<int>();

            if (!es_pelea)
            {
                temporal = celda_id - 29;
                x_temporal = get_Celda_X_Coordenadas(temporal);
                y_temporal = get_Celda_Y_Coordenadas(temporal);
                if (temporal > 1 & temporal < 1024 & x_temporal == x - 1 & y_temporal == y - 1 & !lista_cerrada.Contains(temporal))
                    lista_hijo.Add(temporal);

                temporal = celda_id + 29;
                x_temporal = get_Celda_X_Coordenadas(temporal);
                y_temporal = get_Celda_Y_Coordenadas(temporal);
                if (temporal > 1 & temporal < 1024 & x_temporal == x + 1 & y_temporal == y + 1 & !lista_cerrada.Contains(temporal))
                    lista_hijo.Add(temporal);
            }

            temporal = celda_id - 15;
            x_temporal = get_Celda_X_Coordenadas(temporal);
            y_temporal = get_Celda_Y_Coordenadas(temporal);
            if (temporal > 1 & temporal < 1024 & x_temporal == x - 1 & y_temporal == y & !lista_cerrada.Contains(temporal))
                lista_hijo.Add(temporal);

            temporal = celda_id + 15;
            x_temporal = get_Celda_X_Coordenadas(temporal);
            y_temporal = get_Celda_Y_Coordenadas(temporal);
            if (temporal > 1 & temporal < 1024 & x_temporal == x + 1 & y_temporal == y & !lista_cerrada.Contains(temporal))
                lista_hijo.Add(temporal);

            temporal = celda_id - 14;
            x_temporal = get_Celda_X_Coordenadas(temporal);
            y_temporal = get_Celda_Y_Coordenadas(temporal);
            if (temporal > 1 & temporal < 1024 & x_temporal == x & y_temporal == y - 1 & !lista_cerrada.Contains(temporal))
                lista_hijo.Add(temporal);

            temporal = celda_id + 14;
            x_temporal = get_Celda_X_Coordenadas(temporal);
            y_temporal = get_Celda_Y_Coordenadas(temporal);
            if (temporal > 1 & temporal < 1024 & x_temporal == x & y_temporal == y + 1 & !lista_cerrada.Contains(temporal))
                lista_hijo.Add(temporal);

            if (!es_pelea)
            {
                temporal = celda_id - 1;
                x_temporal = get_Celda_X_Coordenadas(temporal);
                y_temporal = get_Celda_Y_Coordenadas(temporal);
                if (temporal > 1 & temporal < 1024 & x_temporal == x - 1 & y_temporal == y + 1 & !lista_cerrada.Contains(temporal))
                    lista_hijo.Add(temporal);

                temporal = celda_id + 1;
                x_temporal = get_Celda_X_Coordenadas(temporal);
                y_temporal = get_Celda_Y_Coordenadas(temporal);
                if (temporal > 1 & temporal < 1024 & x_temporal == x + 1 & y_temporal == y - 1 & !lista_cerrada.Contains(temporal))
                    lista_hijo.Add(temporal);
            }
            return lista_hijo;
        }

        private int get_F_Punto()
        {
            int x = 9999;
            int cell = 0;

            foreach (int item in lista_abierta)
            {
                if (!lista_cerrada.Contains(item))
                {
                    if (Flist[item] < x)
                    {
                        x = Flist[item];
                        cell = item;
                    }
                }
            }
            return cell;
        }

        public static int get_Celda_Numero(int total_celdas, string celda_char)
        {
            for (int i = 0; i < total_celdas; i++)
            {
                if (get_Celda_Char(i) == celda_char)
                {
                    return i;
                }
            }
            return -1;
        }

        public int get_Celda_Y_Coordenadas(int celda_id)
        {
            int loc5 = celda_id / ((mapa.anchura * 2) - 1);
            int loc6 = celda_id - (loc5 * ((mapa.anchura * 2) - 1));
            int loc7 = loc6 % mapa.anchura;
            return loc5 - loc7;
        }

        public int get_Celda_X_Coordenadas(int celda_id) => (celda_id - ((mapa.anchura - 1) * get_Celda_Y_Coordenadas(celda_id))) / mapa.anchura;

        public int get_Distancia_Estimada(int celda_1, int celda_2)
        {
            if (celda_1 == celda_2)
                return 0;

            int diferencia_x = Math.Abs(get_Celda_X_Coordenadas(celda_1) - get_Celda_X_Coordenadas(celda_2));
            int diferencia_y = Math.Abs(get_Celda_Y_Coordenadas(celda_1) - get_Celda_Y_Coordenadas(celda_2));
            return diferencia_x + diferencia_y;
        }

        public int get_Orientacion_Casilla(int celda_1, int celda_2)
        {
            int mapa_anchura = mapa.anchura;
            int[] _loc6_ = { 1, mapa_anchura, (mapa_anchura * 2) - 1, mapa_anchura - 1, -1, -mapa_anchura, (-mapa_anchura * 2) + 1, -(mapa_anchura - 1) };
            int _loc7_ = celda_2 - celda_1;

            for (int i = 7; i >= 0; i += -1)
            {
                if (_loc6_[i] == _loc7_)
                    return i;
            }

            int resultado_x = get_Celda_X_Coordenadas(celda_2) - get_Celda_X_Coordenadas(celda_1);
            int resultado_y = get_Celda_Y_Coordenadas(celda_2) - get_Celda_Y_Coordenadas(celda_1);

            if (resultado_x == 0)
            {
                if (resultado_y > 0)
                    return 3;
                return 7;
            }
            else if (resultado_x > 0)
            {
                return 1;
            }
            else
            {
                return 5;
            }
        }

        public int get_Tiempo_Desplazamiento(int casilla_inicio, int casilla_final, Direcciones orientacion)
        {
            int distancia = get_Distancia_Estimada(casilla_inicio, casilla_final);
            switch (orientacion)
            {
                case Direcciones.ESTE:
                case Direcciones.OESTE:
                    return 50 + Math.Abs(casilla_inicio - casilla_final) * Convert.ToInt32(distancia >= 4 ? 875d / 2.5d : 875d);

                case Direcciones.NORTE:
                case Direcciones.SUR:
                    return 50 + Math.Abs(casilla_inicio - casilla_final) / ((mapa.anchura * 2) - 1) * Convert.ToInt32(distancia >= 4 ? 875d / 2.5d : 875d);

                case Direcciones.NORDESTE:
                case Direcciones.SUDESTE:
                    return 50 + Math.Abs(casilla_inicio - casilla_final) / (mapa.anchura - 1) * Convert.ToInt32(distancia >= 4 ? 625d / 2.5d : 625d);


                case Direcciones.NOROESTE:
                case Direcciones.SUDOESTE:
                    return 50 + Math.Abs(casilla_inicio - casilla_final) / (mapa.anchura - 1) * Convert.ToInt32(distancia >= 4 ? 625d / 2.5d : 625d);
            }
            return 0;
        }

        private string get_Pathfinding_Limpio(string pathfinding)
        {
            StringBuilder pathfinding_limpio = new StringBuilder();

            if (pathfinding.Length >= 3)
            {
                for (int i = 0; i <= pathfinding.Length - 1; i += 3)
                {
                    if (!pathfinding.get_Substring_Seguro(i, 1).Equals(pathfinding.get_Substring_Seguro(i + 3, 1)))
                    {
                        pathfinding_limpio.Append(pathfinding.get_Substring_Seguro(i, 3));
                    }
                }
            }
            else
            {
                pathfinding_limpio.Append(pathfinding);
            }
            return pathfinding_limpio.ToString();
        }
    }
}