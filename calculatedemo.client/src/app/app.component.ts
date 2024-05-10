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
  public inputPlaceholder = "Enter your expression to calculate here. You can use numbers with or without decimal point, parentheses and arithmetic operators: +, -, * and /";
  public cleareExporessionOnCalculate = true;
  public history: CalculationExt[] = [];
  public predefinedList = [
    "(12 + 34 + 5*6 - 14/7)/5",
    "1234",
    "123 / 45",
    "150 / (45 + 5)",
    "(10 + 5) * 10 / (45 + 5)",
    "(10 + 5) * 10 / (45 + (1 + 4))",
    "(10 + 5)10",
    "(10 + 5) * 10 + Pi",
    "(10 + 5) * 10 + Ln(10)",
    "(10 + 5) * 10 + 3x"
  ]
  isHoverOver = false;
  public get title() {
    return this.apiService.isFaking
      ? "Calculations are faked"
      : "Calculations are happening on the server side"
  }
  constructor(private apiService: ApiService) { }

  ngOnInit() { }

  ngAfterViewInit() {
    this.setFocusOnInput();
  }

  calculate() {
    if (!this.expression) {
      this.showToast("Error", "Expression can not be empty", "error");
    }
    else {
      this.history.unshift(new CalculationExt(this.expression, this.apiService));
      if (this.expression.length <= 100) {
        this.predefinedList = this.predefinedList.filter(x => x !== this.expression);
        this.predefinedList.unshift(this.expression);
      }
      else {
        this.showToast("History", "The expression was not saved to the drop-down list due to its length, but it is still available on the screen for your review and reuse", "info");
      }
    }
    if (this.cleareExporessionOnCalculate) {
      this.expression = "";
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
      timer: 5000,
      title,
      text,
      icon
    });
  }

  expandExpressionValue() {
    this.expression = Array(1000).fill(this.expression).join(' + ');
    this.isHoverOver = false;
  }
}
