﻿using System;
using Bot_Dofus_1._29._1.Otros.Entidades.Stats;

namespace Bot_Dofus_1._29._1.Otros.Entidades
{
    public interface Entidad : IDisposable
    {
        int id { get; set; }
        int celda_id { get; set; }
        CaracteristicasInformacion caracteristicas { get; set; }
    }
}