﻿using System.Collections.Generic;
using static MoneyGrip.Data.Enums;

namespace MoneyGrip.ViewModels
{
    public class SpaardoelPostModel
    {
        public int Id { get; set; }
        public List<int> Label { get; set; }
        public byte? Percentage { get; set; }
        public int? Eindbedrag { get; set; }
        public Maand EersteMaand { get; set; }
        public Maand LaatsteMaand { get; set; }
        public string Omschrijving { get; set; }
    }
}
