import random

st = ""

for _ in range(40):
  x = list(range(9))
  random.shuffle(x)
  sampling = random.sample(x, 5)
  for s in sampling:
    st += (str(s + 1) + ',')
  st = st[:-1]
  st += '\n'

print(st)

f = open("gameTask.csv", "w")
f.write(st)
f.close()
