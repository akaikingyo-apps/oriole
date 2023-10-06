
module example.calculator
{
	uses oriole.core;
	uses oriole.windows;

	class Calculator inherits Form
	{
		field operand;
		field result;
		field operator;
		field operatorPressed;

		static field version;

		static
		{
			Calculator::version = "Calculator Version 0.1";
		}

		static method main(args)
		{
			Console::println(Calculator::version);
			Application::start(new Calculator());
		}

		method onLoad(sender, data)
		{	
			this.setText(Calculator::version);
			this.setSize(200, 300);
			this.result = this.createTextBox("", 10, 10, 160, 10);
			callback = new Callback(this, "onButtonClick(2):1");
			digits = "7,8,9,/,4,5,6,*,1,2,3,-,0,CE,=,+";
			digits = digits.split(',');
			width = 40;
			loop (n = 0 to 16 exclusive)
			{
				x = (width + 1) * (n % 4);
				y = (width + 1) * (n / 4);
				this.createButton(digits[n], 10 + x, 50 + y, width, width, callback);
			}

			this.operand = empty;
			this.operator = empty;
			this.operatorPressed = false;
			textbox = this.result;
			textbox.setText("0");
		}

		method compute(leftValue, rightValue)
		{
			left = Integer::parse(leftValue);
			right = Integer::parse(rightValue);
			if (this.operator == "+")
			{
				this.operand = left + right;
			}
			else if (this.operator == "-")
			{
				this.operand = left - right;
			}
			else if (this.operator == "*")
			{
				this.operand = left * right;
			}
			else 
			{
				this.operand = left / right;
			}
			return empty + this.operand;
		}

		method onButtonClick(sender, data)
		{
			command = sender.getText();
			textbox = this.result;

			if (command == "CE")
			{
				this.operator = empty;
				this.operand = empty;
				textbox.setText("0");
			}
			else if (command == "+" || command == "-" || command == "/" || command == "*")
			{
				if (this.operator != empty)
				{
					this.operand  = this.compute(this.operand, textbox.getText());
				}
				this.operator = command;
				this.operatorPressed = true;
			}
			else if (command == "=")
			{
				if (this.operator != empty)
				{
					this.operand  = this.compute(this.operand, textbox.getText());
				}
				textbox.setText(this.operand);
			}
			else
			{
				if (this.operatorPressed)
				{
					this.operand = textbox.getText();
					textbox.setText(command);
					this.operatorPressed = false;
				}
				else
				{					
					text = textbox.getText();
					if (text == "0")
					{
						text = empty;
					}
					textbox.setText(text + command);
				}
			}
		}
	}
}