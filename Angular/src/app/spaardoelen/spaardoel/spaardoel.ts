import { Label } from "../../labels/label/label";
import { Maanden } from "../../maanden.enum";
import { CurrencyPipe } from '../../currency.pipe';

export class Spaardoel
{
    id: number;
    label: Label[];
    percentage: number;
    eindbedrag: number;
    eersteMaand: Maanden;
    laatsteMaand: Maanden;
    omschrijving: string;

    constructor(private customCurrency: CurrencyPipe) {}

    getValue(value: string) : any
    {
      switch(value)
      {
        case "Label":
            var returnList: string[] = [];
            this.label.forEach(element => {
                returnList.push(element.naam);
            });
            return returnList.join(", ");
          break;
        case "Eindbedrag": return this.eindbedrag ? "€ " + this.customCurrency.transform(this.eindbedrag) : "";
          break;
        case "Percentage": return this.percentage ? this.percentage + "%" : "";
          break;
        case "Eerste maand": return Maanden[this.eersteMaand];
          break;
        case "Laatste maand": return Maanden[this.laatsteMaand];
          break;
        case "Omschrijving": return this.omschrijving;
          break;
        case "Id": return this.id;
          break;
        default: return "";
      }
    }
}