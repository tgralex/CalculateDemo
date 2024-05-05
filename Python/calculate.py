import json
import re

class Calculator:
    calculationSuccess = False
    calculationResult = None
    errorMessage = None

    def __init__(self, expression):
        self.expression = str(expression)

    def calculate(self):
        #Sanitizing the input string by removing every character which is not digit, decimal point or operation sign
        #Leaving all the spaces though - TA
        expression = re.sub(r'[^\d.+*/\-()\s]', '', self.expression)   
        if self.expression != expression:
            self.calculationSuccess = False
            self.calculationResult = None
            self.errorMessage = "Invalid characters were found in the expression"
            return

        
        # Calculate the expression using eval
        try:
            self.calculationResult = eval(expression)
            self.calculationSuccess = True
            self.errorMessage = None
            return
        except Exception as e:
            self.calculationResult = None
            self.calculationSuccess = False
            self.errorMessage = "Calculation has failed"
            return
        
    def get_response(self):
        self.calculate()
        data = {
            "calculationSuccess": self.calculationSuccess,
            "calculationResult": self.calculationResult,
            "errorMessage": self.errorMessage
        }
        return {
            'statusCode': 200,
            'body': data
        }

# calc = Calculator("12*(3+4)")
# print(calc.get_response())