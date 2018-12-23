﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        private Nodo[] posicion_celda { get; }
        private Mapa mapa { get; }
        private readonly bool es_pelea;
        private List<Nodo> lista_celdas_no_permitidas = new List<Nodo>();
        private List<Nodo> lista_celdas_permitidas = new List<Nodo>();
        private StringBuilder camino = new StringBuilder();

        public Pathfinding(Mapa _mapa, bool _es_pelea, bool esquivar_monstruos)
        {
            mapa = _mapa;
            posicion_celda = new Nodo[mapa.celdas.Length];
            rellenar_cuadricula();
            cargar_Obstaculos(esquivar_monstruos);
            es_pelea = _es_pelea;
        }

        private void rellenar_cuadricula()
        {
            for (int i = 0; i < mapa.celdas.Length; i++)
            {
                var tmpCell = mapa.celdas[i];
                posicion_celda[i] = new Nodo(i, get_Celda_X_Coordenadas(i), get_Celda_Y_Coordenadas(i), tmpCell.tipo != TipoCelda.NO_CAMINABLE);
            }
        }

        private void cargar_Obstaculos(bool esquivar_monstruos)
        {
            for (int i = 0; i < mapa.celdas.Length; i++)
            {
                if (mapa.celdas[i].tipo == TipoCelda.OBJETO_INTERACTIVO)
                {
                    lista_celdas_no_permitidas.Add(posicion_celda[i]);
                }
                if (mapa.celdas[i].object2Movement)
                {
                    lista_celdas_no_permitidas.Add(posicion_celda[i]);
                }
            }
            if(esquivar_monstruos)
            {
                mapa.get_Monstruos().ToList().ForEach(monstruo =>
                {
                    get_Celda_Siguiente(posicion_celda[monstruo.Value]).ForEach(celda_monstruo =>
                    {
                        lista_celdas_no_permitidas.Add(posicion_celda[celda_monstruo.id]);
                    });
                });
            }
        }

        public bool get_Camino(int celda_inicio, int celda_final)
        {
            Nodo inicio = posicion_celda[celda_inicio];
            Nodo final = posicion_celda[celda_final];
            lista_celdas_permitidas.Add(inicio);

            while (lista_celdas_permitidas.Count > 0)
            {
                int index = 0;
                for (int i = 1; i < lista_celdas_permitidas.Count; i++)
                {
                    if (lista_celdas_permitidas[i].coste_f < lista_celdas_permitidas[index].coste_f)
                        index = i;

                    if (lista_celdas_permitidas[i].coste_f != lista_celdas_permitidas[index].coste_f) continue;
                    if (lista_celdas_permitidas[i].coste_g > lista_celdas_permitidas[index].coste_g)
                        index = i;

                    if (lista_celdas_permitidas[i].coste_g == lista_celdas_permitidas[index].coste_g)
                        index = i;
                }

                Nodo actual = lista_celdas_permitidas[index];
                if (actual == final)
                {
                    get_Camino_Retroceso(inicio, final);
                    return true;
                }
                lista_celdas_permitidas.Remove(actual);
                lista_celdas_no_permitidas.Add(actual);

                foreach (Nodo celda_siguiente in get_Celda_Siguiente(actual))
                {
                    if (lista_celdas_no_permitidas.Contains(celda_siguiente) || !celda_siguiente.es_caminable) continue;

                    int temporal_g = actual.coste_g + get_Distancia(celda_siguiente, actual);
                    if (!lista_celdas_permitidas.Contains(celda_siguiente))
                    {
                        lista_celdas_permitidas.Add(celda_siguiente);
                    }
                    else if (temporal_g >= celda_siguiente.coste_g)
                        continue;

                    celda_siguiente.coste_g = temporal_g;
                    celda_siguiente.coste_h = get_Distancia(celda_siguiente, final);
                    celda_siguiente.coste_f = celda_siguiente.coste_g + celda_siguiente.coste_h;
                    celda_siguiente.nodo_padre = actual;
                }
            }
            return false;
        }

        private void get_Camino_Retroceso(Nodo nodo_inicial, Nodo nodo_final)
        {
            List<int> lista_celdas_camino = new List<int>();
            var nodo_actual = nodo_final;

            while (nodo_actual != nodo_inicial)
            {
                lista_celdas_camino.Add(nodo_actual.id);
                nodo_actual = nodo_actual.nodo_padre;
            }
            lista_celdas_camino.Add(nodo_inicial.id);
            lista_celdas_camino.Reverse();

            int celda_actual, celda_siguiente = 0;
            for (int i = 0; i < lista_celdas_camino.Count - 1; i++)
            {
                celda_actual = lista_celdas_camino[i];
                celda_siguiente = lista_celdas_camino[i + 1];
                camino.Append(get_Direccion_Char(get_Orientacion_Casilla(celda_actual, celda_siguiente))).Append(get_Celda_Char(celda_siguiente));
            }
        }

        public List<Nodo> get_Celda_Siguiente(Nodo node)
        {
            List<Nodo> celdas_siguientes = new List<Nodo>();

            Nodo celda_derecha = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) + 1 && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id));
            Nodo celda_izquierda = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) - 1 && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id));
            Nodo celda_inferior = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id) + 1);
            Nodo celda_superior = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id) - 1);

            if (celda_derecha != null)
                celdas_siguientes.Add(celda_derecha);
            if (celda_izquierda != null)
                celdas_siguientes.Add(celda_izquierda);
            if (celda_inferior != null)
                celdas_siguientes.Add(celda_inferior);
            if (celda_superior != null)
                celdas_siguientes.Add(celda_superior);

            if (es_pelea)
            {
                //Diagonales
                Nodo celda_superior_izquierda = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) - 1 && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id) - 1);
                Nodo celda_inferior_derecha = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) + 1 && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id) + 1);
                Nodo celda_inferior_izquierda = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) - 1 && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id) + 1);
                Nodo celda_superior_derecha = posicion_celda.FirstOrDefault(nodec => get_Celda_X_Coordenadas(nodec.id) == get_Celda_X_Coordenadas(node.id) + 1 && get_Celda_Y_Coordenadas(nodec.id) == get_Celda_Y_Coordenadas(node.id) - 1);

                if (celda_superior_izquierda != null)
                    celdas_siguientes.Add(celda_superior_izquierda);
                if (celda_inferior_derecha != null)
                    celdas_siguientes.Add(celda_inferior_derecha);
                if (celda_inferior_izquierda != null)
                    celdas_siguientes.Add(celda_inferior_izquierda);
                if (celda_superior_derecha != null)
                    celdas_siguientes.Add(celda_superior_derecha);
            }
            return celdas_siguientes;
        }

        public static string get_Direccion_Char(int direccion)
        {
            if (direccion >= Hash.caracteres_array.Length)
                return string.Empty;
            return Hash.caracteres_array[direccion].ToString();
        }

        public int get_Celda_Y_Coordenadas(int celda_id)
        {
            int loc5 = celda_id / ((mapa.anchura * 2) - 1);
            int loc6 = celda_id - (loc5 * ((mapa.anchura * 2) - 1));
            int loc7 = loc6 % mapa.anchura;
            return loc5 - loc7;
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
                else
                    return 7;
            }
            else if (resultado_x > 0)
                return 1;
            else
                return 5;
        }

        public static string get_Celda_Char(int celda)
        {
            int CharCode2 = celda % Hash.caracteres_array.Length;
            int CharCode1 = (celda - CharCode2) / Hash.caracteres_array.Length;
            return Hash.caracteres_array[CharCode1].ToString() + Hash.caracteres_array[CharCode2].ToString();
        }

        public int get_Distancia_Estimada(int celda_1, int celda_2)
        {
            if (celda_1 != celda_2)
            {
                int diferencia_x = Math.Abs(get_Celda_X_Coordenadas(celda_1) - get_Celda_X_Coordenadas(celda_2));
                int diferencia_y = Math.Abs(get_Celda_Y_Coordenadas(celda_1) - get_Celda_Y_Coordenadas(celda_2));
                return diferencia_x + diferencia_y;
            }
            else
                return 0;
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

        public int get_Tiempo_Desplazamiento_Pelea(int casilla_inicio, int casilla_final, Direcciones orientacion)
        {
            int distancia = get_Distancia_Estimada(casilla_inicio, casilla_final);
            switch (orientacion)
            {
                case Direcciones.ESTE:
                case Direcciones.OESTE:
                    return Math.Abs(casilla_inicio - casilla_final) * Convert.ToInt32(distancia >= 4 ? 875d / 2.5d : 875d);

                case Direcciones.NORTE:
                case Direcciones.SUR:
                    return Math.Abs(casilla_inicio - casilla_final) / ((mapa.anchura * 2) - 1) * Convert.ToInt32(distancia >= 4 ? 875d / 2.5d : 875d);

                case Direcciones.NORDESTE:
                case Direcciones.SUDESTE:
                    return Math.Abs(casilla_inicio - casilla_final) / (mapa.anchura - 1) * Convert.ToInt32(distancia >= 4 ? 625d / 2.5d : 625d);


                case Direcciones.NOROESTE:
                case Direcciones.SUDOESTE:
                    return Math.Abs(casilla_inicio - casilla_final) / (mapa.anchura - 1) * Convert.ToInt32(distancia >= 4 ? 625d / 2.5d : 625d);
            }
            return 0;
        }

        public int get_Celda_X_Coordenadas(int celda_id) => (celda_id - ((mapa.anchura - 1) * get_Celda_Y_Coordenadas(celda_id))) / mapa.anchura;
        public int get_Distancia(Nodo a, Nodo b) => (int)Math.Sqrt(((a.posicion_x - b.posicion_x) * (a.posicion_x - b.posicion_x)) + ((a.posicion_y - b.posicion_y) * (a.posicion_y - b.posicion_y)));

        public string get_Pathfinding_Limpio()
        {
            StringBuilder pathfinding_limpio = new StringBuilder();

            if (camino.ToString().Length >= 3)
            {
                for (int i = 0; i <= camino.ToString().Length - 1; i += 3)
                {
                    if (!camino.ToString().get_Substring_Seguro(i, 1).Equals(camino.ToString().get_Substring_Seguro(i + 3, 1)))
                    {
                        pathfinding_limpio.Append(camino.ToString().get_Substring_Seguro(i, 3));
                    }
                }
            }
            else
            {
                pathfinding_limpio.Append(camino.ToString());
            }
            return pathfinding_limpio.ToString();
        }
    }
}
