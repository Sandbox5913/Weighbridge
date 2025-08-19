// Weighbridge/Behaviors/EventToCommandBehavior.cs
using System.Reflection;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Weighbridge.Behaviors
{
    public class EventToCommandBehavior : Behavior<VisualElement>
    {
        public static readonly BindableProperty EventNameProperty =
            BindableProperty.Create(nameof(EventName), typeof(string), typeof(EventToCommandBehavior), null);

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EventToCommandBehavior), null);

        public string EventName
        {
            get => (string)GetValue(EventNameProperty);
            set => SetValue(EventNameProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        private Delegate _eventHandler;

        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);

            EventInfo eventInfo = bindable.GetType().GetEvent(EventName);
            if (eventInfo == null)
            {
                throw new ArgumentException($"EventToCommandBehavior: Can't find an event named '{EventName}' on '{bindable.GetType().Name}'.");
            }

            MethodInfo methodInfo = typeof(EventToCommandBehavior).GetMethod(nameof(OnEvent), BindingFlags.NonPublic | BindingFlags.Instance);
            _eventHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);
            eventInfo.AddEventHandler(bindable, _eventHandler);
        }

        protected override void OnDetachingFrom(VisualElement bindable)
        {
            base.OnDetachingFrom(bindable);
            if (_eventHandler != null)
            {
                EventInfo eventInfo = bindable.GetType().GetEvent(EventName);
                eventInfo?.RemoveEventHandler(bindable, _eventHandler);
            }
            _eventHandler = null;
        }

        private void OnEvent(object sender, EventArgs e)
        {
            if (Command?.CanExecute(null) == true)
            {
                Command.Execute(null);
            }
        }
    }
}