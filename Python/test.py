import unittest
from calculate import Calculator

class TestCalculations(unittest.TestCase):
    def test_success_calculations(self):
        test_success_expressions = [
            ("12+14/2", 19.0), 
            ("12 +     14/2   ", 19.0), 
            ("12 + 31 - 5 * 2", 33),
            ("12 - 5 * 2", 2),
            ("1.2 * 3 - 0.6 * 5", 0.6),
            ("12 - 5 * 2.4", 0),
        ]
        for expression_tuple in test_success_expressions:
            expression = expression_tuple[0]
            expected_result = expression_tuple[1]
            calc = Calculator(expression)
            calc.calculate()
            self.assertAlmostEqual(calc.calculationResult, expected_result, 5, 'The calculation was incorrect. We got an error message: '+str(calc.errorMessage))


    def test_failing_calculations(self):
        test_failing_expressions = [
            "12 - 5 / 0",
            "12+14/2a - x",
            "12 + 31 - 5 * 2 / Pi",
            "12 * 3 - 6 * 5 Ln(10)",
            "asdfghjkl",
            None,
            "",
            "/",
            "**.-+"
        ]
        for expression in test_failing_expressions:
            calc = Calculator(expression)
            self.assertEqual(calc.calculationResult, None, 'The calculation was expected to be incorrect, but we got a successful result = '+str(calc.calculationResult))

if __name__ == '__main__':
    unittest.main()