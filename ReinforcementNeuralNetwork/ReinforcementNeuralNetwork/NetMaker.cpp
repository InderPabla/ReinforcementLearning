#include "stdafx.h"
#include <iostream>
#include "floatfann.h"
#include "fann_cpp.h"

using namespace std;

class NetMaker {
	private:
		int numberOfInputNeurons;
		int numberOfOutputNeurons;
		int numberOfHiddenLayers;
		int numberOfHiddenNeurons;

	public:
		NetMaker(int inputs, int outputs, int layers, int hiddens) {
			printf("%i %i %i %i",inputs,outputs,layers,hiddens);
			//cout << inputs+" "+outputs+" "+layers+" "+hiddens;
			
		}
};