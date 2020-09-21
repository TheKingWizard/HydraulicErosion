# Hydraulic Erosion

## This is an erosion simulation for procedurally generated terrain. 

It was inspired by this [this](https://www.youtube.com/watch?v=eaXk97ujbPQ) video by Sebastian Lague. 
The implementation follows the algorithm detailed [here](https://www.firespark.de/resources/downloads/implementation%20of%20a%20methode%20for%20hydraulic%20erosion.pdf) by Hans Theobald Beyer. 

It has been modified to work on a graph of arbitrarily connected nodes (regions) as opposed to the original grid implementation, for use in another project I'm working on. A GPU implementation has been added to significantly increase the speed of the simulation as it takes a rather long time to run on the CPU. Due to the added complexity of the arbitrary geometry, the GPU implementation lacks the *erosion radius* feature, but I hope to add that in the future. 