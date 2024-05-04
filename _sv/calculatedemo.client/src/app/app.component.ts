import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CalculationExt } from './classes/calculation-ext';
import { ApiService } from './services/api.service';
import Swal, { SweetAlertIcon } from 'sweetalert2'

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, AfterViewInit {
  @ViewChild("expressionInput") expressionInput?: ElementRef;

  public expression?: string;
  public history: CalculationExt[] = [];
  public predefinedList = [
    "(12 + 34 + 5*6 - 14/7)/5",
    "1234",
    "123 / 45",
    "150 / (45 + 5)",
    "(10 + 5) * 10 / (45 + 5)"
  ]
  public get title() {
    return this.apiService.isFaking
      ? "Calculations are faked"
      : "Calculations are happening on the server side"
  }
  constructor(private apiService: ApiService) { }

  ngOnInit() {
    this.expression = "(1+2)*30/4       -     1/(1+3)"
  }

  ngAfterViewInit() {
    this.setFocusOnInput();
  }

  calculate() {
    if (!this.expression) {
      this.showToast("Error", "Expression can not be empty", "error");
    }
    else {
      this.history.unshift(new CalculationExt(this.expression, this.apiService));
    }
    this.setFocusOnInput();
  }

  clear() {
    this.expression = "";
    this.history = [];
    this.setFocusOnInput();
  }

  readFrom(el: CalculationExt) {
    this.expression = el.expression;
    this.setFocusOnInput();
  }

  setFocusOnInput() {
    setTimeout(() => {
      this.expressionInput?.nativeElement.focus();
    }, 0);
  }

  toggleFaking() {
    this.apiService.isFaking ? this.apiService.stopFaking() : this.apiService.fakeIt(20, 1000, 3);
    if (this.apiService.isFaking) {
      this.expression = "123";
      this.showToast("Calculations", "Faking Server side calculations: Result=20, Delay=1 sec, every 3-rd request will fail", "warning");
    }
    else {
      this.showToast("Calculations", "Server side calculations are restored", "info");
    }
  }

  showToast(title: string, text: string, icon: SweetAlertIcon) {
    Swal.fire({
      toast: true,
      position: 'bottom-right',
      showConfirmButton: false,
      timer: 3000,
      title,
      text,
      icon
    });
  }

}



