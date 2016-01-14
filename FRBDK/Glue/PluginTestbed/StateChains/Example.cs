//using FlatRedBall.Instructions;

//namespace PluginTestbed.StateChains
//{
//    public class Example
//    {
//        public enum StateChains
//        {
//            None = 0,
//            Test1,
//            Test2
//        }

//        private StateChains _currentStateChain = StateChains.None;
//        private int _index;
//        private MethodInstruction<Test1> _instruction;

//        public StateChains CurrentStateChain
//        {
//            get { return _currentStateChain; }
            
//            set
//            {
//                StopStateChain();

//                _currentStateChain = value;
//                _index = 0;

//                switch (_currentStateChain)
//                {
//                    case StateChains.Test1:
//                        StartNextStateTest1();
//                        break;
//                    case StateChains.Test2:
//                        StartNextStateTest2();
//                        break;
//                }
//            }
//        }

//        public void ManageStateChains()
//        {
//            if(CurrentStateChain == StateChains.None) return;

//            switch (CurrentStateChain)
//            {
//                case StateChains.Test1:

//                    if(_index == 0 && CurrentStateChain == VariableState.Test1)
//                    {
//                        _index++;
//                        StartNextStateTest1();
//                    }
//                    else if (_index == 1 && CurrentStateChain == VariableState.Test2)
//                    {
//                        _index++;
//                        StartNextStateTest1();
//                    }
//                    else if (_index == 2 && CurrentStateChain == VariableState.Test3)
//                    {
//                        _index++;
//                        StartNextStateTest1();
//                    }
//                    else if (_index == 3 && CurrentStateChain == VariableState.Test4)
//                    {
//                        _index++;
//                        StartNextStateTest1();
//                    }

//                    break;
//            }
//        }

//        public void StopStateChain()
//        {
//            if (CurrentStateChain == StateChains.None) return;

//            switch (CurrentStateChain)
//            {
//                case StateChains.Test1:

//                    if (_index == 0)
//                    {
//                        Instructions.Remove(_instruction);
//                        StopStateInterpolation(VariableState.Test1);
//                    }
//                    else if (_index == 1)
//                    {
//                        Instructions.Remove(_instruction);
//                        StopStateInterpolation(VariableState.Test2);
//                    }
//                    else if (_index == 2)
//                    {
//                        Instructions.Remove(_instruction);
//                        StopStateInterpolation(VariableState.Test3);
//                    }
//                    else if (_index == 3)
//                    {
//                        Instructions.Remove(_instruction);
//                        StopStateInterpolation(VariableState.Test1);
//                    }

//                    break;
//            }

//            _instruction = null;
//        }

//        private void StartNextStateTest1()
//        {
//            if(_index < 0)
//            {
//                _index = 0;
//            }

//            if(_index > 3)
//            {
//                _index = 0;
//            }

//            switch (_index)
//            {
//                case 0:
//                    _instruction = InterpolateToState(VariableState.Test1, 1);
//                    break;
//                case 1:
//                    _instruction = InterpolateToState(VariableState.Test2, 1);
//                    break;
//                case 2:
//                    _instruction = InterpolateToState(VariableState.Test3, 1);
//                    break;
//                case 3:
//                    _instruction = InterpolateToState(VariableState.Test4, 1);
//                    break;
//            }
//        }
//    }
//}
