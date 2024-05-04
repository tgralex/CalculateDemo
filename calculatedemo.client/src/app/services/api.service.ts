import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';
import { Calculation } from '../classes/calculation';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  basePath?: string;
  error = false;

  // next variables are for faking the service
  amount = 0;
  delayMs = 0;
  isFaking = false;
  calls = 0;
  failEveryXTimes = 2;

  constructor(private http: HttpClient, private httpClient: HttpClient) {
    if (location.href.indexOf("localhost") > 0) {
      this.httpClient.get('assets/base_path.txt', { responseType: 'text' }).subscribe(
        {
          next: x => this.basePath = x.trim(),
          error: _ => this.error = true
        }
      );
    }
    else {
      this.basePath = ""
    }
  }

  public calculate(expression: string): Observable<Calculation> {
    if (this.isFaking) {
      return this.getFakeObservable();
    }
    if (this.basePath !== undefined) {
      return this.http.put<Calculation>(this.basePath + "/api/Calculate", { expression });
    }
    var calc = new Calculation();
    calc.calculationSuccess = false;
    calc.errorMessage = "Setup error - base path is not defined"
    return of(calc);
  }

  public fakeIt(amount: number, delayMs: number, failEveryXTimes: number) {
    this.amount = amount;
    this.delayMs = delayMs;
    this.isFaking = true;
    this.failEveryXTimes = failEveryXTimes;
  }

  public stopFaking() {
    this.isFaking = false;
  }

  private getFakeObservable(): Observable<Calculation> {
    var calc = new Calculation();
    calc.calculationSuccess = ++this.calls % this.failEveryXTimes !== 0;
    calc.calculationResult = calc.calculationSuccess ? this.amount : undefined;
    calc.errorMessage = calc.calculationSuccess ? "" : "Scheduled calculation failure";

    return of(calc).pipe(delay(this.delayMs));
  }
}
