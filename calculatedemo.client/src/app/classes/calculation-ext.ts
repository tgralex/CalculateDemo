import { take } from "rxjs";
import { ApiService } from "../services/api.service";
import { Calculation } from "./calculation";
import { HttpErrorResponse } from "@angular/common/http";

export class CalculationExt extends Calculation {
  public callInProgress = true;
  public get title() { return this.errorMessage ? this.errorMessage : "Ok" }
  public get cssClass() { return this.callInProgress ? "calc-in-progress" : this.calculationSuccess ? "calc-success" : "calc-danger" }

  constructor(public expression: string, apiService: ApiService) {
    super();
    apiService.calculate(expression)
      .pipe(take(1))
      .subscribe({
        next: (x) => {
          this.calculationSuccess = x.calculationSuccess;
          this.calculationResult = x.calculationResult;
          this.errorMessage = x.errorMessage;
        },
        error: (err: HttpErrorResponse) => {
          console.log('Error', err);
          this.errorMessage = "There was an error connecting to the server";
          this.callInProgress = false;
        },
        complete: () => this.callInProgress = false
      });
  }
}
