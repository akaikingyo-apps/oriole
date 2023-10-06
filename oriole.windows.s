
module oriole.windows
{	
	uses oriole.core;
	uses oriole.collections;

	facade class _WUI ("Oriole.Library.Core.WindowsFacadeClass", "Core.dll");
	
	enum EventType
    {
        None = 0,
        FormClosing = 1,
        Click = 2,
        TextChanged = 3,
        Resize = 4,
        MouseClick = 5,
        MouseDoubleClick = 6,
        MouseEnter = 7,
        MouseLeave = 8,
        MouseHover = 9,
        MouseMove = 10,
        KeyPress = 11,
        GetFocus = 12,
        LostFocus = 13
    }

	class EventData
	{
	}

	class MouseEventData inherits EventData
	{
		field x;
		field y;
	}

	class KeyEventData inherits EventData
	{
		field key;
	}

	class Application
	{
		static field _active;
		static field _controls;

		static
		{
			Application::_controls = new Dictionary();
		}

		static method registerControl(handler, control)
		{
			controls = Application::_controls;
			controls.set(handler, control);
		}

		static method exit()
		{
			Application::_active = false;
		}

		static method start(form)
		{
			Application::_active = true;

			if (!(form is Form))
			{
				throw new Exception("Application::start() expects a form instance.");
			}

			form.onLoad(form, new EventData());
			form.show();

			while (Application::_active)
			{
				if (_WUI::hasEvent())
				{
					event = _WUI::getEvent();
					eventType = event[0];
					handle = event[1];
					rawData = event[2];
					data = new EventData();
					controls = Application::_controls;
					control = controls.get(handle);
					handler = null;
					
					switch (eventType)
					{
						case EventType::FormClosing:
							handler = control.onClosing;
							break;
						case EventType::Click:
							handler = control.onClick;
							break;
						case EventType::TextChanged:
							handler = control.onTextChanged;
							break;
						case EventType::Resize:
							handler = control.onResize;
							break;
						case EventType::MouseClick:
							handler = control.onMouseClick;
							data = new MouseEventData();
							data.x = rawData[0];
							data.y = rawData[1];
							break;
						case EventType::MouseDoubleClick:
							handler = control.onMouseDoubleClick;
							data = new MouseEventData();
							data.x = rawData[0];
							data.y = rawData[1];
							break;
						case EventType::MouseEnter:
							handler = control.onMouseEnter;
							break;
						case EventType::MouseLeave:
							handler = control.onMouseLeave;
							break;
						case EventType::MouseHover:
							handler = control.onMouseHover;
							break;
						case EventType::MouseMove:
							handler = control.onMouseMove;
							data = new MouseEventData();
							data.x = rawData[0];
							data.y = rawData[1];
							break;
						case EventType::KeyPress:
							handler = control.onKeyPress;
							data = new KeyEventData();
							data.key = rawData[0];
							break;
						case EventType::GetFocus:
							handler = control.onGetFocus;
							break;
						case EventType::LostFocus:
							handler = control.onLostFocus;
							break;
					}
					
					if (handler != null)
					{
						parameters = new [2];
						parameters[0] = control;
						parameters[1] = data;
						handler.invoke(parameters);
					}			
				}
				else
				{
					Thread::nice();
				}
			}

			form.close();
		}
	}

	class Control
	{
		field _handler;
		field _controls;
		
		field onClick;
		field onTextChanged;
		field onResize;
		field onMouseClick;
		field onMouseDoubleClick;
		field onMouseEnter;
		field onMouseLeave;
		field onMouseHover;
		field onMouseMove;
		field onKeyPress;
		field onGetFocus;
		field onLostFocus;

		constructor()
		{
			//Console::println("Control::cstr(): creating control ..");			
			this._controls = new List();
		}

		method setPosition(x, y)
		{
			_WUI::setPosition(this._handler, x, y);	
		}

		method setSize(width, height)
		{
			_WUI::setSize(this._handler, width, height);	
		}

		method addControl(control)
		{
			if (!(control is Control))
			{
				throw new Exception("Form::add() expects a control instance.");
			}
			c = this._controls;
			c.add(control);
			_WUI::addControl(this._handler, control._handler);
		}

		method getText()
		{
			return _WUI::getText(this._handler);
		}

		method setText(text)
		{
			_WUI::setText(this._handler, text);
		}

		method show()
		{
			foreach (child in this._controls)
			{
				child.show();
			}
			_WUI::showControl(this._handler);
		}

		method hide()
		{			
			foreach (child in this._controls)
			{
				child.hide();
			}
			_WUI::hideControl(this._handler);
		}

		method dispose()
		{	
			foreach (child in this._controls)
			{
				child.dispose();
			}
			_WUI::dispose(this._handler);
		}
	}

	class Form inherits Control
	{
		field onClosing;

		constructor(): super()
		{	
			//Console::println("Form::cstr(): creating form ..");
			this._handler = _WUI::createForm();	
			Application::registerControl(this._handler, this);
			//Console::println("Form::cstr(): handler=" + this._handler);
			this.onClosing = new Callback(this, "onClosing(2):1");
		}

		method onLoad(sender, data)
		{
		}	

		method onClosing(sender, data)
		{
			Application::exit();
		}

		method onClosed(sender, data)
		{
		}

		method close()
		{
			this.dispose();
		}		

		// utilities

		method createTextBox(text, x, y, width, height)
		{
			control = new TextBox();
			control.setPosition(x, y);
			control.setSize(width, height);
			control.setText(text);
			this.addControl(control);
			return control;
		}

		method createButton(text, x, y, width, height, onClick)
		{
			control = new Button();
			control.setPosition(x, y);
			control.setSize(width, height);
			control.setText(text);
			control.onClick = onClick;
			this.addControl(control);
			return control;
		}
	}

	class Button inherits Control
	{
		constructor(): super()
		{
			//Console::println("Button::cstr(): creating control ..");			
			this._handler = _WUI::createButton();
			Application::registerControl(this._handler, this);
			//Console::println("Button::cstr(): handler=" + this._handler);
		}		
	}

	class TextBox inherits Control
	{
		constructor(): super()
		{
			//Console::println("TextBox::cstr(): creating control ..");			
			this._handler = _WUI::createTextBox();
			Application::registerControl(this._handler, this);
			//Console::println("TextBox::cstr(): handler=" + this._handler);
		}
	}

	class Label inherits Control
	{
		constructor(): super()
		{
			//Console::println("Label::cstr(): creating control ..");			
			this._handler = _WUI::createLabel();
			Application::registerControl(this._handler, this);	
			//Console::println("Label::cstr(): handler=" + this._handler);
		}
	}

	class ComboBox inherits Control
	{
		constructor(): super()
		{
			//Console::println("ComboBox::cstr(): creating control ..");			
			this._handler = _WUI::createComboBox();
			Application::registerControl(this._handler, this);	
			//Console::println("ComboBox::cstr(): handler=" + this._handler);
		}

		method addItem(item)
		{
			_WUI::addComboBoxItem(this._handler, item);
		}
	}
}
