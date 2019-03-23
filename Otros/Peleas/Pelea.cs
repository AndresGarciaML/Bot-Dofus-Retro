﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bot_Dofus_1._29._1.Otros.Entidades.Personajes.Hechizos;
using Bot_Dofus_1._29._1.Otros.Mapas;
using Bot_Dofus_1._29._1.Otros.Peleas.Enums;
using Bot_Dofus_1._29._1.Otros.Peleas.Peleadores;
using Bot_Dofus_1._29._1.Protocolo.Enums;

/*
    Este archivo es parte del proyecto BotDofus_1.29.1

    BotDofus_1.29.1 Copyright (C) 2019 Alvaro Prendes — Todos los derechos reservados.
    Creado por Alvaro Prendes
    web: http://www.salesprendes.com
*/

namespace Bot_Dofus_1._29._1.Otros.Peleas
{
    public class Pelea : IDisposable
    {
        private Cuenta cuenta;
        private ConcurrentDictionary<int, Luchadores> luchadores;
        private ConcurrentDictionary<int, Luchadores> enemigos;
        private ConcurrentDictionary<int, Luchadores> aliados;
        private Dictionary<int, int> hechizos_intervalo;// hechizoID, turnos intervalo
        private Dictionary<int, int> total_hechizos_lanzados;//hechizoID, total veces
        private Dictionary<int, Dictionary<int, int>> total_hechizos_lanzados_en_celda;//hechizoID (celda, veces)
        public LuchadorPersonaje jugador_luchador { get; private set; }
        private bool disposed;

        public IEnumerable<Luchadores> get_Aliados => aliados.Values.Where(a => a.esta_vivo);
        public IEnumerable<Luchadores> get_Enemigos => enemigos.Values.Where(e => e.esta_vivo);
        public IEnumerable<Luchadores> get_Luchadores => luchadores.Values.Where(f => f.esta_vivo);
        public int total_enemigos_vivos => get_Enemigos.Count(f => f.esta_vivo);
        public List<int> get_Celdas_Ocupadas => get_Luchadores.Select(f => f.celda_id).ToList();

        public event Action pelea_creada;
        public event Action pelea_iniciada;
        public event Action pelea_acabada;
        public event Action turno_iniciado;

        public Pelea(Cuenta _cuenta)
        {
            cuenta = _cuenta;
            luchadores = new ConcurrentDictionary<int, Luchadores>();
            enemigos = new ConcurrentDictionary<int, Luchadores>();
            aliados = new ConcurrentDictionary<int, Luchadores>();
            hechizos_intervalo = new Dictionary<int, int>();
            total_hechizos_lanzados = new Dictionary<int, int>();
            total_hechizos_lanzados_en_celda = new Dictionary<int, Dictionary<int, int>>();
        }

        public void get_Lanzar_Hechizo(int hechizo_id, int celda_id)
        {
            if (cuenta.Estado_Cuenta == EstadoCuenta.LUCHANDO)
            {
                Hechizo hechizo = cuenta.personaje.hechizos.FirstOrDefault(f => f.id == hechizo_id);
                HechizoStats datos_hechizo = hechizo.get_Hechizo_Stats()[hechizo.nivel];

                cuenta.conexion.enviar_Paquete("GA300" + hechizo.id + ';' + celda_id);

                if (datos_hechizo.intervalo > 0 && !hechizos_intervalo.ContainsKey(hechizo.id))
                {
                    hechizos_intervalo.Add(hechizo.id, datos_hechizo.intervalo);
                }

                if (!total_hechizos_lanzados.ContainsKey(hechizo.id))
                    total_hechizos_lanzados.Add(hechizo.id, 0);
                total_hechizos_lanzados[hechizo.id]++;

                if (total_hechizos_lanzados_en_celda.ContainsKey(hechizo.id))
                {
                    if (!total_hechizos_lanzados_en_celda[hechizo.id].ContainsKey(celda_id))
                        total_hechizos_lanzados_en_celda[hechizo.id].Add(celda_id, 0);
                    total_hechizos_lanzados_en_celda[hechizo.id][celda_id]++;
                }
                else
                {
                    total_hechizos_lanzados_en_celda.Add(hechizo.id, new Dictionary<int, int>()
                    {
                        { celda_id, 1 }
                    });
                }
            }
        }

        public void get_Final_Turno(int id_personaje)
        {
            Luchadores luchador = get_Luchador_Por_Id(id_personaje);

            if (luchador == jugador_luchador)
            {
                total_hechizos_lanzados.Clear();
                total_hechizos_lanzados_en_celda.Clear();

                for (int i = hechizos_intervalo.Count - 1; i >= 0; i--)
                {
                    int key = hechizos_intervalo.ElementAt(i).Key;
                    hechizos_intervalo[key]--;
                    if (hechizos_intervalo[key] == 0)
                        hechizos_intervalo.Remove(key);
                }
            }
        }

        public Luchadores get_Luchador_Por_Id(int id)
        {
            if (jugador_luchador != null && jugador_luchador.id == id)
            {
                return jugador_luchador;
            }
            else if (luchadores.TryGetValue(id, out Luchadores luchador))
            {
                return luchador;
            }
            return null;
        }

        public Luchadores get_Enemigo_Mas_Debil()
        {
            int vida = -1;
            Luchadores enemigo = null;

            foreach (Luchadores luchador_enemigo in get_Enemigos)
            {
                if (luchador_enemigo.esta_vivo)
                {
                    if (vida == -1 || luchador_enemigo.porcentaje_vida < vida)
                    {
                        vida = luchador_enemigo.porcentaje_vida;
                        enemigo = luchador_enemigo;
                    }
                }
            }
            return enemigo;
        }

        public Luchadores get_Obtener_Aliado_Mas_Cercano(Mapa mapa)
        {
            int distancia = -1, distancia_temporal;
            Luchadores aliado = null;

            foreach (Luchadores luchador_aliado in get_Aliados)
            {
                if (luchador_aliado.esta_vivo)
                {
                    distancia_temporal = mapa.get_Distancia_Entre_Dos_Casillas(jugador_luchador.celda_id, luchador_aliado.celda_id);

                    if (distancia == -1 || distancia_temporal < distancia)
                    {
                        distancia = distancia_temporal;
                        aliado = luchador_aliado;
                    }
                }
            }
            return aliado;
        }

        public Luchadores get_Obtener_Enemigo_Mas_Cercano(Mapa mapa)
        {
            int distancia = -1, distancia_temporal;
            Luchadores enemigo = null;
            
            foreach (Luchadores luchador_enemigo in get_Enemigos)
            {
                if (luchador_enemigo.esta_vivo)
                {
                    distancia_temporal = mapa.get_Distancia_Entre_Dos_Casillas(jugador_luchador.celda_id, luchador_enemigo.celda_id);

                    if (distancia == -1 || distancia_temporal < distancia)
                    {
                        distancia = distancia_temporal;
                        enemigo = luchador_enemigo;
                    }
                }
            }
            return enemigo;
        }

        public void get_Agregar_Luchador(Luchadores luchador)
        {
            if (luchador.id == cuenta.personaje.id)
                jugador_luchador = new LuchadorPersonaje(cuenta.personaje.nombre_personaje, cuenta.personaje.nivel, luchador);
            else if (!luchadores.TryAdd(luchador.id, luchador))
                luchador.get_Actualizar_Luchador(luchador.id, luchador.esta_vivo, luchador.vida_actual, luchador.pa, luchador.pm, luchador.celda_id, luchador.vida_maxima, luchador.equipo);

            get_Ordenar_Luchadores();
        }

        private void get_Ordenar_Luchadores()
        {
            if (jugador_luchador != null)
            {
                foreach (Luchadores luchador in get_Luchadores)
                {
                    if (!aliados.ContainsKey(luchador.id) || !enemigos.ContainsKey(luchador.id))
                    {
                        if (luchador.equipo == jugador_luchador.equipo)
                            aliados.TryAdd(luchador.id, luchador);
                        else
                            enemigos.TryAdd(luchador.id, luchador);
                    }
                }
            }
        }

        public Luchadores get_Luchador_Esta_En_Celda(int celda_id)
        {
            if (jugador_luchador?.celda_id == celda_id)
                return jugador_luchador;
            return get_Luchadores.FirstOrDefault(f => f.esta_vivo && f.celda_id == celda_id);
        }

        public bool es_Celda_Libre(int celda_id) => get_Luchador_Esta_En_Celda(celda_id) == null;

        public IEnumerable<Luchadores> get_Cuerpo_A_Cuerpo_Enemigo(int celda_id = -1) => get_Enemigos.Where(e => e.esta_vivo && cuenta.personaje.mapa.get_Distancia_Entre_Dos_Casillas(celda_id == -1 ? jugador_luchador.celda_id : celda_id, e.celda_id) == 1);
        public IEnumerable<Luchadores> get_Cuerpo_A_Cuerpo_Aliado(int celda_id = -1) => get_Aliados.Where(a => a.esta_vivo && cuenta.personaje.mapa.get_Distancia_Entre_Dos_Casillas(celda_id == -1 ? jugador_luchador.celda_id : celda_id, a.celda_id) == 1);
        public bool esta_Cuerpo_A_Cuerpo_Con_Enemigo(int celda_id = -1) => get_Cuerpo_A_Cuerpo_Enemigo(celda_id).Count() > 0;
        public bool esta_Cuerpo_A_Cuerpo_Con_Aliado(int celda_id = -1) => get_Cuerpo_A_Cuerpo_Aliado(celda_id).Count() > 0;

        public FallosLanzandoHechizo get_Puede_Lanzar_hechizo(int hechizo_id)
        {
            Hechizo hechizo = cuenta.personaje.get_Hechizo(hechizo_id);

            if (hechizo == null)
                return FallosLanzandoHechizo.DESONOCIDO;

            HechizoStats datos_hechizo = hechizo.get_Hechizo_Stats()[hechizo.nivel];

            if (jugador_luchador.pa < datos_hechizo.coste_pa)
                return FallosLanzandoHechizo.PUNTOS_ACCION;

            if (datos_hechizo.lanzamientos_por_turno > 0 && total_hechizos_lanzados.ContainsKey(hechizo_id) && total_hechizos_lanzados[hechizo_id] >= datos_hechizo.lanzamientos_por_turno)
                return FallosLanzandoHechizo.DEMASIADOS_LANZAMIENTOS;

            if (hechizos_intervalo.ContainsKey(hechizo_id))
                return FallosLanzandoHechizo.COOLDOWN;

            return FallosLanzandoHechizo.NINGUNO;
        }

        public FallosLanzandoHechizo get_Puede_Lanzar_hechizo(int hechizo_id, int celda_objetivo, Mapa mapa)
        {
            Hechizo hechizo = cuenta.personaje.hechizos.FirstOrDefault(f => f.id == hechizo_id);

            if (hechizo == null)
                return FallosLanzandoHechizo.DESONOCIDO;

            HechizoStats datos_hechizo = hechizo.get_Hechizo_Stats()[hechizo.nivel];

            if (datos_hechizo.lanzamientos_por_objetivo > 0 && total_hechizos_lanzados_en_celda.ContainsKey(hechizo_id) && total_hechizos_lanzados_en_celda[hechizo_id].ContainsKey(celda_objetivo) && total_hechizos_lanzados_en_celda[hechizo_id][celda_objetivo] >= datos_hechizo.lanzamientos_por_objetivo)
                return FallosLanzandoHechizo.DEMASIADOS_LANZAMIENTOS_POR_OBJETIVO;

            if (datos_hechizo.es_celda_vacia && !es_Celda_Libre(celda_objetivo))
                return FallosLanzandoHechizo.NECESITA_CELDA_LIBRE;

            if (datos_hechizo.es_lanzado_linea && !mapa.get_Esta_En_Linea(jugador_luchador.celda_id, celda_objetivo))
                return FallosLanzandoHechizo.NO_ESTA_EN_LINEA;

            if (datos_hechizo.es_lanzado_con_vision && !mapa.get_Verificar_Linea_Vision(jugador_luchador.celda_id, celda_objetivo))
                return FallosLanzandoHechizo.NO_TIENE_LINEA_VISION;

            if (!get_Rango_hechizo(jugador_luchador.celda_id, datos_hechizo, mapa).Contains(celda_objetivo))
                return FallosLanzandoHechizo.NO_ESTA_EN_RANGO;

            return FallosLanzandoHechizo.NINGUNO;
        }

        public List<int> get_Rango_hechizo(int celda_personaje, HechizoStats datos_hechizo, Mapa mapa)
        {
            List<int> rango = new List<int>();

            foreach (var celda in HechizoShape.Get_Lista_Celdas_Rango_Hechizo(celda_personaje, datos_hechizo, mapa, cuenta.personaje.caracteristicas.alcanze.total_stats))
            {
                if (celda == null || rango.Contains(celda.id))
                    continue;

                if (datos_hechizo.es_celda_vacia && get_Celdas_Ocupadas.Contains(celda.id))
                {
                    Console.WriteLine(celda.id);
                    continue;
                }
                if (celda.tipo != TipoCelda.CELDA_CAMINABLE || celda.tipo != TipoCelda.OBJETO_INTERACTIVO)
                    rango.Add(celda.id);
            }

            if (datos_hechizo.es_lanzado_con_vision)
            {
                for (int i = rango.Count - 1; i >= 0; i--)
                {
                    if (get_Linea_Obstruida(mapa, celda_personaje, rango[i], get_Celdas_Ocupadas))
                            rango.RemoveAt(i);
                }
            }
            return rango;
        }

        public static bool get_Linea_Obstruida(Mapa map, int sourceCellId, int targetCellId, List<int> occupiedCells)
        {
            double x = map.get_Celda_X_Coordenadas(sourceCellId) + 0.5;
            double y = map.get_Celda_Y_Coordenadas(sourceCellId) + 0.5;
            double objetivo_x = map.get_Celda_X_Coordenadas(targetCellId) + 0.5;
            double objetivo_y = map.get_Celda_Y_Coordenadas(targetCellId) + 0.5;
            double anterior_x = map.get_Celda_X_Coordenadas(sourceCellId);
            double anterior_y = map.get_Celda_Y_Coordenadas(sourceCellId);

            double padX = 0;
            double padY = 0;
            double steps = 0;
            int cas = 0;

            if (Math.Abs(x - objetivo_x) == Math.Abs(y - objetivo_y))
            {
                steps = Math.Abs(x - objetivo_x);
                padX = (objetivo_x > x) ? 1 : -1;
                padY = (objetivo_y > y) ? 1 : -1;
                cas = 1;
            }
            else if (Math.Abs(x - objetivo_x) > Math.Abs(y - objetivo_y))
            {
                steps = Math.Abs(x - objetivo_x);
                padX = (objetivo_x > x) ? 1 : -1;
                padY = (objetivo_y - y) / steps;
                padY = padY * 100;
                padY = Math.Ceiling(padY) / 100;
                cas = 2;
            }
            else
            {
                steps = Math.Abs(y - objetivo_y);
                padX = (objetivo_x - x) / steps;
                padX = padX * 100;
                padX = Math.Ceiling(padX) / 100;
                padY = (objetivo_y > y) ? 1 : -1;
                cas = 3;
            }

            int errorSup = Convert.ToInt32(Math.Round(Math.Floor(Convert.ToDouble((3 + (steps / 2))))));
            int errorInf = Convert.ToInt32(Math.Round(Math.Floor(Convert.ToDouble((97 - (steps / 2))))));

            for (int i = 0; i < steps; i++)
            {
                double cellX, cellY;
                double xPadX = x + padX;
                double yPadY = y + padY;

                switch (cas)
                {
                    case 2:
                        double beforeY = Math.Ceiling(y * 100 + padY * 50) / 100;
                        double afterY = Math.Floor(y * 100 + padY * 150) / 100;
                        double diffBeforeCenterY = Math.Floor(Math.Abs(Math.Floor(beforeY) * 100 - beforeY * 100)) / 100;
                        double diffCenterAfterY = Math.Ceiling(Math.Abs(Math.Ceiling(afterY) * 100 - afterY * 100)) / 100;

                        cellX = Math.Floor(xPadX);

                        if (Math.Floor(beforeY) == Math.Floor(afterY))
                        {
                            cellY = Math.Floor(yPadY);
                            if ((beforeY == cellY && afterY < cellY) || (afterY == cellY && beforeY < cellY))
                            {
                                cellY = Math.Ceiling(yPadY);
                            }
                            if (get_Es_Celda_Obstruida(cellX, cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = cellY;
                        }
                        else if (Math.Ceiling(beforeY) == Math.Ceiling(afterY))
                        {
                            cellY = Math.Ceiling(yPadY);
                            if ((beforeY == cellY && afterY < cellY) || (afterY == cellY && beforeY < cellY))
                            {
                                cellY = Math.Floor(yPadY);
                            }
                            if (get_Es_Celda_Obstruida(cellX, cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = cellY;
                        }
                        else if (Math.Floor(diffBeforeCenterY * 100) <= errorSup)
                        {
                            if (get_Es_Celda_Obstruida(cellX, Math.Floor(afterY), map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = Math.Floor(afterY);
                        }
                        else if (Math.Floor(diffCenterAfterY * 100) >= errorInf)
                        {
                            if (get_Es_Celda_Obstruida(cellX, Math.Floor(beforeY), map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = Math.Floor(beforeY);
                        }
                        else
                        {
                            if (get_Es_Celda_Obstruida(cellX, Math.Floor(beforeY), map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = Math.Floor(beforeY);
                            if (get_Es_Celda_Obstruida(cellX, Math.Floor(afterY), map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_y = Math.Floor(afterY);
                        }
                        break;

                    case 3:
                        double beforeX = Math.Ceiling(x * 100 + padX * 50) / 100;
                        double afterX = Math.Floor(x * 100 + padX * 150) / 100;
                        double diffBeforeCenterX = Math.Floor(Math.Abs(Math.Floor(beforeX) * 100 - beforeX * 100)) / 100;
                        double diffCenterAfterX = Math.Ceiling(Math.Abs(Math.Ceiling(afterX) * 100 - afterX * 100)) / 100;

                        cellY = Math.Floor(yPadY);

                        if (Math.Floor(beforeX) == Math.Floor(afterX))
                        {
                            cellX = Math.Floor(xPadX);
                            if ((beforeX == cellX && afterX < cellX) || (afterX == cellX && beforeX < cellX))
                            {
                                cellX = Math.Ceiling(xPadX);
                            }
                            if (get_Es_Celda_Obstruida(cellX, cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = cellY;
                        }
                        else if (Math.Ceiling(beforeX) == Math.Ceiling(afterX))
                        {
                            cellX = Math.Ceiling(xPadX);
                            if ((beforeX == cellX && afterX < cellX) || (afterX == cellX && beforeX < cellX))
                            {
                                cellX = Math.Floor(xPadX);
                            }
                            if (get_Es_Celda_Obstruida(cellX, cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = cellX;
                            anterior_y = cellY;
                        }
                        else if (Math.Floor(diffBeforeCenterX * 100) <= errorSup)
                        {
                            if (get_Es_Celda_Obstruida(Math.Floor(afterX), cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = Math.Floor(afterX);
                            anterior_y = cellY;
                        }
                        else if (Math.Floor(diffCenterAfterX * 100) >= errorInf)
                        {
                            if (get_Es_Celda_Obstruida(Math.Floor(beforeX), cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = Math.Floor(beforeX);
                            anterior_y = cellY;
                        }
                        else
                        {
                            if (get_Es_Celda_Obstruida(Math.Floor(beforeX), cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = Math.Floor(beforeX);
                            anterior_y = cellY;
                            if (get_Es_Celda_Obstruida(Math.Floor(afterX), cellY, map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                            anterior_x = Math.Floor(afterX);
                        }
                    break;

                    default:
                        if (get_Es_Celda_Obstruida(Math.Floor(xPadX), Math.Floor(yPadY), map, occupiedCells, targetCellId, anterior_x, anterior_y)) return true;
                        anterior_x = Math.Floor(xPadX);
                        anterior_y = Math.Floor(yPadY);
                    break;
                }

                x = (x * 100 + padX * 100) / 100;
                y = (y * 100 + padY * 100) / 100;
            }
            return false;
        }

        private static bool get_Es_Celda_Obstruida(double x, double y, Mapa map, List<int> occupiedCells, int targetCellId, double lastX, double lastY)
        {
            Celda mp = map.get_Coordenadas((int)x, (int)y);

            if (!mp.es_linea_vision || (mp.id != targetCellId && occupiedCells.Contains(mp.id)))
                return true;
            else
                return false;
        }

        #region Zona Eventos
        public void get_Combate_Creado()
        {
            cuenta.Estado_Cuenta = EstadoCuenta.LUCHANDO;
            cuenta.logger.log_informacion("PELEA", "Nueva pelea iniciada");
            pelea_creada?.Invoke();
        }

        public void get_Combate_Finalizado()
        {
            enemigos.Clear();
            aliados.Clear();
            luchadores.Clear();
            hechizos_intervalo.Clear();
            total_hechizos_lanzados.Clear();
            total_hechizos_lanzados_en_celda.Clear();
            jugador_luchador = null;
            cuenta.Estado_Cuenta = EstadoCuenta.CONECTADO_INACTIVO;
            cuenta.logger.log_informacion("PELEA", "Pelea acabada");
            pelea_acabada?.Invoke();
        }

        public void get_Turno_Iniciado() => turno_iniciado?.Invoke();

        public void get_Turno_Acabado()
        {
            total_hechizos_lanzados.Clear();
            total_hechizos_lanzados_en_celda.Clear();

            for (int i = hechizos_intervalo.Count - 1; i >= 0; i--)
            {
                int key = hechizos_intervalo.ElementAt(i).Key;
                hechizos_intervalo[key]--;
                if (hechizos_intervalo[key] == 0)
                    hechizos_intervalo.Remove(key);
            }
        }
        #endregion

        #region Zona Dispose
        ~Pelea() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                luchadores.Clear();
                enemigos.Clear();
                aliados.Clear();
                total_hechizos_lanzados.Clear();
                hechizos_intervalo.Clear();
                total_hechizos_lanzados_en_celda.Clear();
                cuenta = null;
                luchadores = null;
                enemigos = null;
                aliados = null;
                total_hechizos_lanzados = null;
                hechizos_intervalo = null;
                total_hechizos_lanzados_en_celda = null;
                jugador_luchador = null;
                disposed = true;
            }
        }
        #endregion
    }
}